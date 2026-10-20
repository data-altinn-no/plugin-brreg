using System;
using Dan.Common.Enums;
using Dan.Common.Exceptions;
using Dan.Common.Models;
using Dan.Common.Util;
using Microsoft.Extensions.Options;
using Nadobe.EvidenceSources.ES_BR;
using System.Threading.Tasks;
using Dan.Common.Interfaces;
using Dan.Plugin.Brreg.Config;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Dan.Plugin.Brreg.Models;
using Dan.Plugin.Brreg.Helpers;

namespace Dan.Plugin.Brreg {


    /// <summary>
    /// This class implements the Azure Function entry points for all the functions implemented by this evidence source. 
    /// </summary>  
    public class CertificateOfRegistration
    {
        private readonly Settings _settings;
        private readonly IEvidenceSourceMetadata _metadata;
        private readonly ILogger _logger;
        private readonly HttpClient _client;

        public CertificateOfRegistration(IOptions<Settings> settings, IEvidenceSourceMetadata evidenceSourceMetadata, ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory)
        {
            _settings = settings.Value;
            _metadata = evidenceSourceMetadata;
            _logger = loggerFactory.CreateLogger<CertificateOfRegistration>();
            _client = httpClientFactory.CreateClient("SafeHttpClient");
        }

        /// <summary>
        /// Function entry point: Certificate of Registration
        /// </summary>
        /// <param name="req">
        /// The HTTP request.
        /// </param>
        /// <returns>
        /// A <see cref="HttpResponseMessage"/>.
        /// </returns>
        [Function(nameof(CertificateOfRegistration))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, FunctionContext context)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);
            return await EvidenceSourceResponse.CreateResponse(req, () => GetCertificateOfRegistration(evidenceHarvesterRequest.SubjectParty.NorwegianOrganizationNumber));
        }

        /// <summary>
        /// Function entry point: Certificate of Registration
        /// </summary>
        /// <param name="req">
        /// The HTTP request.
        /// </param>
        /// <returns>
        /// A <see cref="HttpResponseMessage"/>.
        /// </returns>
        [Function("Firmaattest")]
        public async Task<HttpResponseData> Firmaattest([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, FunctionContext context)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);
            return await EvidenceSourceResponse.CreateResponse(req, () => GetCertificateOfRegistration(evidenceHarvesterRequest.SubjectParty.NorwegianOrganizationNumber));
        }

        /// <summary>
        /// The evidence code definition
        /// </summary>
        /// <returns>The definition</returns>
        public static EvidenceCode GetDefinition()
        {
            return new EvidenceCode()
            {
                EvidenceCodeName = nameof(CertificateOfRegistration),
                Description = "Code for retrieving the certificate of registration (firmaattest) as a base64-encoded PDF attachment",
                ServiceContext = "eBevis",
                BelongsToServiceContexts = new List<string>() { Constants.EBEVIS, Constants.SERIOSITET, Constants.EDUEDILIGENCE },
                Values = new List<EvidenceValue>
                {
                    new EvidenceValue()
                    {
                        EvidenceValueName = "CertificateOfRegistrationPdfUrl",
                        ValueType = EvidenceValueType.Uri,
                        Source = Constants.SourceEnhetsregisteret
                    },
                    new EvidenceValue()
                    {
                        EvidenceValueName = "CertificateOfRegistrationPdf",
                        ValueType = EvidenceValueType.Attachment,
                        MimeType = "application/pdf",
                        Source = Constants.SourceEnhetsregisteret
                    }
                },
                AuthorizationRequirements = new List<Requirement>()
                {
                    new PartyTypeRequirement()
                    {
                        AppliesToServiceContext = new List<string>() { Constants.EBEVIS, Constants.EDUEDILIGENCE  },
                        AllowedPartyTypes = new AllowedPartyTypesList()
                        {
                            new KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>(AccreditationPartyTypes.Requestor,PartyTypeConstraint.PublicAgency)
                        }
                    },
                    new PartyTypeRequirement()
                    {
                        AppliesToServiceContext = new List<string>() { Constants.SERIOSITET},
                        AllowedPartyTypes = new AllowedPartyTypesList()
                        {
                            new KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>(AccreditationPartyTypes.Requestor,PartyTypeConstraint.PrivateEnterprise)
                        }
                    }
                }
            };
        }    


        /// <summary>
        /// Returns the firmaattest PDF as an <see cref="EvidenceValueType.Attachment"/>. EvidenceBuilder base64-encodes
        /// the raw bytes. The URL value points at the public lookup page the PDF is generated from.
        /// </summary>
        private async Task<List<EvidenceValue>> GetCertificateOfRegistration(string organization)
        {
            var pdfBytes = await Helpers.Firmaattest.DownloadAsync(_client, organization, _logger);

            var eb = new EvidenceBuilder(_metadata, nameof(CertificateOfRegistration));
            eb.AddEvidenceValue("CertificateOfRegistrationPdfUrl", Helpers.Firmaattest.GetLookupPageUrl(organization), Constants.SourceEnhetsregisteret, false);
            eb.AddEvidenceValue("CertificateOfRegistrationPdf", pdfBytes, Constants.SourceEnhetsregisteret, false);

            return eb.GetEvidenceValues();
        }
    }
}
