using Dan.Common.Enums;
using Dan.Common.Interfaces;
using Dan.Common.Models;
using Dan.Common.Util;
using Dan.Plugin.Brreg.Config;
using Dan.Plugin.Brreg.Models;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
using Dan.Plugin.Brreg.Helpers;
using Dan.Plugin.Brreg.Models.EktepaktV2;
using System.Net.Http;

namespace Dan.Plugin.Brreg
{
    public class Frivillighetsregisteret
    {
        private Settings _settings;
        private IEvidenceSourceMetadata _metadata;
        private ILogger<Frivillighetsregisteret> _logger;
        private HttpClient _client;

        public Frivillighetsregisteret(IOptions<Settings> settings, IEvidenceSourceMetadata evidenceSourceMetadata, ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory)
        {
            _settings = settings.Value;
            _metadata = evidenceSourceMetadata;
            _logger = loggerFactory.CreateLogger<Frivillighetsregisteret>();
            _client = httpClientFactory.CreateClient("SafeHttpClient");
        }

        [Function("FrivilligOrganisasjon")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);
            return await EvidenceSourceResponse.CreateResponse(req, () => GetValuesFrivillig(evidenceHarvesterRequest.SubjectParty.NorwegianOrganizationNumber));
        }

        private async Task<List<EvidenceValue>> GetValuesFrivillig(string organisationNumber)
        {
            var result = new List<EvidenceValue>();

            var url = string.Format(_settings.FrivilligUri, organisationNumber);

            var response = await Requests.GetData<FrivillighetsregisteretResponse>(_client,url, _logger);            

            var ecb = new EvidenceBuilder(_metadata, "FrivilligOrganisasjon");
            ecb.AddEvidenceValue("default", JsonConvert.SerializeObject(response), Constants.SourceFrivillighetsregisteret, false);
            return ecb.GetEvidenceValues();
        }        

        public static EvidenceCode GetDefinitionFrivilligOrganisation()
        {
            return new EvidenceCode()
            {
                EvidenceCodeName = "FrivilligOrganisasjon",
                BelongsToServiceContexts = new List<string>() { Constants.DIGOKFRIV },
                Description = $"Hente en organisasjon fra {Constants.SourceFrivillighetsregisteret}",
                IsPublic = true,
                EvidenceSource = "Brreg",
                Values = new List<EvidenceValue>()
                {
                    new EvidenceValue()
                    {
                        EvidenceValueName = "default",
                        Source = Constants.SourceLosoreregisteret,
                        ValueType = EvidenceValueType.JsonSchema,
                        Description = $"Json payload from {Constants.SourceFrivillighetsregisteret}",
                        JsonSchemaDefintion = EvidenceValue.SchemaFromObject<FrivillighetsregisteretResponse>(),
                    }
                }
            };
        }
    }
}
