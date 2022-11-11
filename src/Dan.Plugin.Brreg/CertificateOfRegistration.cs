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

namespace Dan.Plugin.Brreg {


    /// <summary>
    /// This class implements the Azure Function entry points for all the functions implemented by this evidence source. 
    /// </summary>  
    public class CertificateOfRegistration
    {
        private const int REPORT_CODE = 2000;
        private Settings _settings;
        private readonly IEvidenceSourceMetadata _metadata;

    public CertificateOfRegistration(IOptions<Settings> settings, IEvidenceSourceMetadata evidenceSourceMetadata)
        {
            _settings = settings.Value;
            _metadata = evidenceSourceMetadata;
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
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);
            return await EvidenceSourceResponse.CreateResponse(req, ()=> GetCertificateOfRegistration(evidenceHarvesterRequest.OrganizationNumber));
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
        public async Task<HttpResponseData> Firmaattest([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);
            return await EvidenceSourceResponse.CreateResponse(req, () => GetCertificateOfRegistration(evidenceHarvesterRequest.OrganizationNumber));
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
                Description = "Code for retrieving URL to a PDF for the certificate of registration",
                ServiceContext = "eBevis",
                BelongsToServiceContexts = new List<string>() { "eBevis" },
                Values = new List<EvidenceValue>
                {
                    new EvidenceValue()
                    {
                        EvidenceValueName = "CertificateOfRegistrationPdfUrl",
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

        public static EvidenceCode GetDefinitionSeriositet()
        {
            return new EvidenceCode()
            {
                EvidenceCodeName = "Firmaattest",
                Description = "Code for retrieving URL to a PDF for the certificate of registration",
                BelongsToServiceContexts = {"Seriøsitetsinformasjon"},
                Values = new List<EvidenceValue>
                {
                    new EvidenceValue()
                    {
                        EvidenceValueName = "CertificateOfRegistrationPdfUrl",
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
        private async Task<List<EvidenceValue>> GetCertificateOfRegistration(string organization)
        {
            Product[] availableProducts;
            try
            {
                availableProducts = await BRProductsService.GetProductList(organization, REPORT_CODE, _settings);
            }
            catch (Exception ex)
            {
                throw new EvidenceSourceTransientException(Constants.ERROR_PRODUCT_LIST_NOT_AVAILABLE, "Unable to retrieve product list for this organization");
            }

            if (availableProducts == null || availableProducts.Length != 1)
            {
                throw new EvidenceSourcePermanentClientException(
                    Constants.ERROR_CERTIFICATE_OF_REGISTRATION_NOT_AVAILABLE,
                    "Unable to order certificate of registration for this organization");
            }

            var orderResult = await BRProductsService.OrderProducts(organization, availableProducts, _settings);
            var eb = new EvidenceBuilder(_metadata, nameof(CertificateOfRegistration));

            eb.AddEvidenceValue($"CertificateOfRegistrationPdfUrl", orderResult[0].Url);

            return eb.GetEvidenceValues();
        }
    }
}
