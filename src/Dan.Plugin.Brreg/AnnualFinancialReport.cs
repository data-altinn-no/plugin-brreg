using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace Nadobe.EvidenceSources.ES_BR
{
    /// <summary>
    /// This class implements the Azure Function entry points for all the functions implemented by this evidence source. 
    /// </summary>
    public class AnnualFinancialReport
    {
        private const int MIN_YEARS = 1;
        private const int MAX_YEARS = 5;
        private const int REPORT_CODE = 3001;

        private readonly Settings _settings;
        private ILogger _logger;
        private readonly IEvidenceSourceMetadata _metadata;

        public AnnualFinancialReport(IOptions<Settings> settings, IEvidenceSourceMetadata evidenceSourceMetadata, ILoggerFactory loggerFactory)
        {
            _settings = settings.Value;
            _metadata = evidenceSourceMetadata;
            _logger = loggerFactory.CreateLogger<AnnualFinancialReport>();
        }

        [Function("AnnualFinancialReport")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, FunctionContext context)
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);

            var numberOfYears = int.Parse(evidenceHarvesterRequest.GetParameter("NumberOfYears").Value.ToString());

            if (numberOfYears < MIN_YEARS)
            {
                numberOfYears = MIN_YEARS;
            }
            else if (numberOfYears > MAX_YEARS)
            {
                numberOfYears = MAX_YEARS;
            }

            var organization = evidenceHarvesterRequest.SubjectParty.NorwegianOrganizationNumber;

            return await EvidenceSourceResponse.CreateResponse(req, ()=> GetAnnualFinancialReports(organization, numberOfYears));                
        }

        [Function("Aarsregnskap")]
        public async Task<HttpResponseData> RunAarsregnskapAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, FunctionContext context)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);

            var numberOfYears = int.Parse(evidenceHarvesterRequest.GetParameter("NumberOfYears").Value.ToString());

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
                BelongsToServiceContexts = new List<string>() { "eBevis"},
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
                        AllowedPartyTypes = new AllowedPartyTypesList()
                        {
                            new KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>(AccreditationPartyTypes.Requestor,PartyTypeConstraint.PublicAgency)
                        }
                    }
                }
            };
        }

        /// <summary>
        /// The evidence code definition
        /// </summary>
        /// <returns>The definition</returns>
        public static EvidenceCode GetDefinitionSeriositet()
        {
            return new EvidenceCode
            {
                EvidenceCodeName = "Aarsregnskap",
                Description = "Code for retrieving URLs to PDFs for annual financial reports (1-5 years) synchronously",
                IsAsynchronous = false,
                BelongsToServiceContexts = new List<string>() { "Seriøsitetsinformasjon" },
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
                        AllowedPartyTypes = new AllowedPartyTypesList()
                        {
                            new KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>(AccreditationPartyTypes.Requestor,PartyTypeConstraint.PrivateEnterprise)
                        }
                    }
                }
            };
        }

        private async Task<List<EvidenceValue>> GetAnnualFinancialReports(string organization, int numberOfYears)
        {
            Product[] annualReports = await BRProductsService.GetProductList(organization, REPORT_CODE, _settings);
            if (annualReports == null)
            {
                throw new EvidenceSourcePermanentClientException(
                    Constants.ERROR_NO_REPORT_AVAILABLE,
                    $"No financial reports are available for {organization}");
            }

            Product[] listToBeOrdered =
                (from t in annualReports where t.Code == REPORT_CODE orderby t.accountYear descending select t)
                .Take(numberOfYears).ToArray();

            if (!listToBeOrdered.Any())
            {
                throw new EvidenceSourcePermanentClientException(
                    Constants.ERROR_NO_REPORT_AVAILABLE,
                    $"No financial reports are available for {organization}");
            }

            OrderedProduct[] orderResult = await BRProductsService.OrderProducts(organization, listToBeOrdered, _settings);

            var eb = new EvidenceBuilder(_metadata, nameof(AnnualFinancialReport));

            for (int i = 0; i < orderResult.Length; i++)
            {
                eb.AddEvidenceValue($"Year{i + 1}", orderResult[i].AccountYear);
                eb.AddEvidenceValue($"Year{i + 1}PdfUrl", orderResult[i].Url);
            }

            return eb.GetEvidenceValues();
        }
    }
}
