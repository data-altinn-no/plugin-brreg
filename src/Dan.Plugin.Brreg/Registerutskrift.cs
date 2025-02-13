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
    public class Registerutskrift
    {
        private const int REPORT_CODE = 5040;
        private Settings _settings;
        private readonly IEvidenceSourceMetadata _metadata;
        private readonly ILogger _logger;

    public Registerutskrift(IOptions<Settings> settings, IEvidenceSourceMetadata evidenceSourceMetadata, ILoggerFactory loggerFactory)
        {
            _settings = settings.Value;
            _metadata = evidenceSourceMetadata;
            _logger = loggerFactory.CreateLogger<Registerutskrift>();
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
        [Function(nameof(Registerutskrift))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);
            return await EvidenceSourceResponse.CreateResponse(req, ()=> GetRegistrationPrint(evidenceHarvesterRequest.SubjectParty.NorwegianOrganizationNumber));
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
                Description = "Provides a URL to a PDF for the certificate of registration",
                BelongsToServiceContexts = new List<string>() { Constants.DIGOKFRIV },
                IsPublic = true,
                Values = new List<EvidenceValue>
                {
                    new EvidenceValue()
                    {
                        EvidenceValueName = "CertificateUrl",
                        ValueType = EvidenceValueType.Uri,
                        Source = Constants.SourceEnhetsregisteret
                    }
                }             
            };
        }      


        private async Task<List<EvidenceValue>> GetRegistrationPrint(string organization)
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
                    Constants.ERROR_CERTIFICATE_NOT_AVAILABLE,
                    "Unable to order print-out for this organization");
            }

            var orderResult = await BRProductsService.OrderProducts(organization, availableProducts, _settings);
            var eb = new EvidenceBuilder(_metadata, nameof(Registerutskrift));

            eb.AddEvidenceValue($"CertificateUrl", orderResult[0].Url);

            return eb.GetEvidenceValues();
        }
    }
}
