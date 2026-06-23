using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Dan.Plugin.Brreg.Config;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Dan.Common.Exceptions;
using Dan.Common.Models;
using Dan.Common.Util;
using Newtonsoft.Json;
using Microsoft.Azure.Functions.Worker.Http;
using Dan.Common.Extensions;
using Dan.Common.Enums;
using Dan.Common.Interfaces;
using Dan.Plugin.Brreg;
using Dan.Plugin.Brreg.Models;
using Dan.Plugin.Brreg.Helpers;

namespace Nadobe.EvidenceSources.ES_BR
{
    /// <summary>
    /// This class implements the Azure Function entry points for all the functions implemented by this evidence source.
    /// </summary>
    public class AnnualFinancialReport
    {
        private const int MIN_YEARS = 1;
        private const int MAX_YEARS = 5;

        private readonly Settings _settings;
        private ILogger _logger;
        private readonly IEvidenceSourceMetadata _metadata;
        private readonly HttpClient _client;

        public AnnualFinancialReport(IOptions<Settings> settings, IEvidenceSourceMetadata evidenceSourceMetadata, ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory)
        {
            _settings = settings.Value;
            _metadata = evidenceSourceMetadata;
            _logger = loggerFactory.CreateLogger<AnnualFinancialReport>();
            _client = httpClientFactory.CreateClient("SafeHttpClient");
        }

        [Function("AnnualFinancialReport")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, FunctionContext context)
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);

            evidenceHarvesterRequest.TryGetParameter("NumberOfYears", out int numberOfYears);

            if (numberOfYears < MIN_YEARS)
            {
                numberOfYears = MIN_YEARS;
            }
            else if (numberOfYears > MAX_YEARS)
            {
                numberOfYears = MAX_YEARS;
            }

            var organization = evidenceHarvesterRequest.SubjectParty.NorwegianOrganizationNumber;
            return await EvidenceSourceResponse.CreateResponse(req, () => GetAnnualFinancialReports(organization, numberOfYears));
        }

        [Function("Aarsregnskap")]
        public async Task<HttpResponseData> RunAarsregnskapAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, FunctionContext context)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);

            evidenceHarvesterRequest.TryGetParameter("NumberOfYears", out int numberOfYears);

            if (numberOfYears < MIN_YEARS)
            {
                numberOfYears = MIN_YEARS;
            }
            else if (numberOfYears > MAX_YEARS)
            {
                numberOfYears = MAX_YEARS;
            }

            var organization = evidenceHarvesterRequest.SubjectParty.NorwegianOrganizationNumber;

            return await EvidenceSourceResponse.CreateResponse(req, () => GetAnnualFinancialReports(organization, numberOfYears));
        }

        [Function("AnnualFinancialReportPdf")]
        public async Task<HttpResponseData> RunPdfAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, FunctionContext context)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);

            evidenceHarvesterRequest.TryGetParameter("Year", out string year);

            if (string.IsNullOrEmpty(year) || !System.Text.RegularExpressions.Regex.IsMatch(year, @"^\d{4}$"))
            {
                throw new EvidenceSourcePermanentClientException(
                    Constants.ERROR_NO_REPORT_AVAILABLE,
                    "Year parameter is missing or invalid. Expected a 4-digit year.");
            }

            var organization = evidenceHarvesterRequest.SubjectParty.NorwegianOrganizationNumber;

            string url = $"{_settings.RegnskapsregisteretUri}/regnskapsregisteret/regnskap/aarsregnskap/kopi/{organization}/{year}";

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            requestMessage.Headers.Accept.Clear();
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

            if (!string.IsNullOrEmpty(_settings.RegnskapsregisteretUsername))
            {
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.RegnskapsregisteretUsername}:{_settings.RegnskapsregisteretPw}"));
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", authString);
            }

            var apiResponse = await _client.SendAsync(requestMessage);

            if (!apiResponse.IsSuccessStatusCode)
            {
                throw new EvidenceSourcePermanentClientException(
                    Constants.ERROR_NO_REPORT_AVAILABLE,
                    $"No PDF available for {organization} year {year}");
            }

            var pdfBytes = await apiResponse.Content.ReadAsByteArrayAsync();

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/pdf");
            await response.Body.WriteAsync(pdfBytes);
            return response;
        }

        /// <summary>
        /// The evidence code definition
        /// </summary>
        /// <returns>The definition</returns>
        public static EvidenceCode GetDefinition()
        {
            return new EvidenceCode
            {
                EvidenceCodeName = nameof(AnnualFinancialReport),
                Description = "Code for retrieving URLs to PDFs for annual financial reports (1-5 years) synchronously",
                IsAsynchronous = false,
                BelongsToServiceContexts = new List<string>() { Constants.EBEVIS, Constants.SERIOSITET, Constants.EDUEDILIGENCE },
                Parameters = new List<EvidenceParameter>
                {
                    new EvidenceParameter
                    {
                        EvidenceParamName = "NumberOfYears",
                        ParamType = EvidenceParamType.Number,
                        Required = true
                    }
                },
                Values = new List<EvidenceValue>
                {
                    new EvidenceValue
                    {
                        EvidenceValueName = "Year1",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },
                    new EvidenceValue
                    {
                        EvidenceValueName = "Year1PdfUrl",
                        ValueType = EvidenceValueType.Uri,
                        Source = Constants.SourceEnhetsregisteret
                    },
                    new EvidenceValue
                    {
                        EvidenceValueName = "Year2",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },
                    new EvidenceValue
                    {
                        EvidenceValueName = "Year2PdfUrl",
                        ValueType = EvidenceValueType.Uri,
                        Source = Constants.SourceEnhetsregisteret
                    },
                    new EvidenceValue
                    {
                        EvidenceValueName = "Year3",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },
                    new EvidenceValue
                    {
                        EvidenceValueName = "Year3PdfUrl",
                        ValueType = EvidenceValueType.Uri,
                        Source = Constants.SourceEnhetsregisteret
                    },
                    new EvidenceValue
                    {
                        EvidenceValueName = "Year4",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },
                    new EvidenceValue
                    {
                        EvidenceValueName = "Year4PdfUrl",
                        ValueType = EvidenceValueType.Uri,
                        Source = Constants.SourceEnhetsregisteret
                    },
                    new EvidenceValue
                    {
                        EvidenceValueName = "Year5",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },
                    new EvidenceValue
                    {
                        EvidenceValueName = "Year5PdfUrl",
                        ValueType = EvidenceValueType.Uri,
                        Source = Constants.SourceEnhetsregisteret
                    }
                },
                AuthorizationRequirements = new List<Requirement>()
                {
                    new PartyTypeRequirement()
                    {
                        AppliesToServiceContext = new List<string>() { Constants.EBEVIS, Constants.EDUEDILIGENCE },
                        AllowedPartyTypes = new AllowedPartyTypesList()
                        {

                            new KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>(AccreditationPartyTypes.Requestor,PartyTypeConstraint.PublicAgency)
                        }
                    },
                    new PartyTypeRequirement()
                    {
                        AppliesToServiceContext = new List<string>() { Constants.SERIOSITET },
                        AllowedPartyTypes = new AllowedPartyTypesList()
                        {

                            new KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>(AccreditationPartyTypes.Requestor,PartyTypeConstraint.PrivateEnterprise)
                        }
                    },
                    new AccreditationPartyRequirement()
                    {
                        AppliesToServiceContext = new List<string>() { Constants.EDUEDILIGENCE, Constants.SERIOSITET },
                        PartyRequirements = new List<AccreditationPartyRequirementType>()
                        {
                            AccreditationPartyRequirementType.RequestorAndOwnerAreEqual
                        }
                    }
                }
            };
        }
        
        public static EvidenceCode GetDefinitionPdf()
        {
            return new EvidenceCode
            {
                EvidenceCodeName = "AnnualFinancialReportPdf",
                Description = "Returns the PDF for an annual financial report for a given year",
                IsAsynchronous = false,
                BelongsToServiceContexts = new List<string>() { Constants.EBEVIS, Constants.SERIOSITET, Constants.EDUEDILIGENCE },
                Parameters = new List<EvidenceParameter>
                {
                    new EvidenceParameter
                    {
                        EvidenceParamName = "Year",
                        ParamType = EvidenceParamType.String,
                        Required = true
                    }
                },
                Values = new List<EvidenceValue>
                {
                    new EvidenceValue
                    {
                        EvidenceValueName = "pdf",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceRegnskapsregisteret
                    }
                },
                AuthorizationRequirements = new List<Requirement>()
                {
                    new PartyTypeRequirement()
                    {
                        AppliesToServiceContext = new List<string>() { Constants.EBEVIS, Constants.EDUEDILIGENCE },
                        AllowedPartyTypes = new AllowedPartyTypesList()
                        {
                            new KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>(AccreditationPartyTypes.Requestor, PartyTypeConstraint.PublicAgency)
                        }
                    },
                    new PartyTypeRequirement()
                    {
                        AppliesToServiceContext = new List<string>() { Constants.SERIOSITET },
                        AllowedPartyTypes = new AllowedPartyTypesList()
                        {
                            new KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>(AccreditationPartyTypes.Requestor, PartyTypeConstraint.PrivateEnterprise)
                        }
                    },
                    new AccreditationPartyRequirement()
                    {
                        AppliesToServiceContext = new List<string>() { Constants.EDUEDILIGENCE, Constants.SERIOSITET },
                        PartyRequirements = new List<AccreditationPartyRequirementType>()
                        {
                            AccreditationPartyRequirementType.RequestorAndOwnerAreEqual
                        }
                    }
                }
            };
        }

        private async Task<List<EvidenceValue>> GetAnnualFinancialReportPdf(string organization, string year)
        {
            string url = $"{_settings.RegnskapsregisteretUri}/regnskapsregisteret/regnskap/aarsregnskap/kopi/{organization}/{year}";

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            requestMessage.Headers.Accept.Clear();
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

            if (!string.IsNullOrEmpty(_settings.RegnskapsregisteretUsername))
            {
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.RegnskapsregisteretUsername}:{_settings.RegnskapsregisteretPw}"));
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", authString);
            }

            var response = await _client.SendAsync(requestMessage);

            if (!response.IsSuccessStatusCode)
            {
                throw new EvidenceSourcePermanentClientException(
                    Constants.ERROR_NO_REPORT_AVAILABLE,
                    $"No PDF available for {organization} year {year}");
            }

            var pdfBytes = await response.Content.ReadAsByteArrayAsync();

            var eb = new EvidenceBuilder(_metadata, "AnnualFinancialReportPdf");
            eb.AddEvidenceValue("pdf", Convert.ToBase64String(pdfBytes), Constants.SourceRegnskapsregisteret, false);

            return eb.GetEvidenceValues();
        }

        private async Task<List<EvidenceValue>> GetAnnualFinancialReports(string organization, int numberOfYears)
        {
            string url = $"{_settings.RegnskapsregisteretUri}/regnskapsregisteret/regnskap/aarsregnskap/kopi/{organization}/aar";

            var response = await Requests.MakeRequest(url, _client,
                _settings.RegnskapsregisteretUsername, _settings.RegnskapsregisteretPw,
                HttpMethod.Get, _logger);

            List<string> availableYears = JsonConvert.DeserializeObject<List<string>>(
                    JsonConvert.SerializeObject(response));            

            if (availableYears == null || !availableYears.Any())
            {
                throw new EvidenceSourcePermanentClientException(
                    Constants.ERROR_NO_REPORT_AVAILABLE,
                    $"No financial reports are available for {organization}");
            }

            var yearsToReturn = availableYears
                .OrderByDescending(y => y)
                .Take(numberOfYears)
                .ToList();

            var eb = new EvidenceBuilder(_metadata, nameof(AnnualFinancialReport));

            for (int i = 0; i < yearsToReturn.Count; i++)
            {
                string year = yearsToReturn[i];
                string pdfUrl = $"{_settings.RegnskapsregisteretUri}/regnskapsregisteret/regnskap/aarsregnskap/kopi/{organization}/{year}";
                eb.AddEvidenceValue($"Year{i + 1}", year);
                eb.AddEvidenceValue($"Year{i + 1}PdfUrl", pdfUrl);
            }
            return eb.GetEvidenceValues();
        }
    }
}
