using Dan.Common.Enums;
using Dan.Common.Exceptions;
using Dan.Common.Interfaces;
using Dan.Plugin.Brreg.Config;
using Dan.Plugin.Brreg.Models;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Options;
using Dan.Common.Models;
using Dan.Common.Util;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Dan.Plugin.Brreg.Helpers;
using Dan.Plugin.Brreg.Models.EktepaktV2;
using NJsonSchema;


namespace Nadobe.EvidenceSources.ES_BR
{

    /// <summary>
    /// This class implements the Azure Function entry points for all the functions implemented by this evidence source. 
    /// </summary>   
    public class Ektepakt
    {
        private HttpClient _maskinportenClient;
        private Settings _settings;
        private readonly IEvidenceSourceMetadata _metadata;
        private readonly ILogger _logger;

        public Ektepakt(IHttpClientFactory clientFactory, IOptions<Settings> settings, IEvidenceSourceMetadata evidenceSourceMetadata, ILoggerFactory loggerFactory)
        {
            _maskinportenClient = clientFactory.CreateClient("myMaskinportenClient");
            _settings = settings.Value;
            _metadata = evidenceSourceMetadata;
            _logger = loggerFactory.CreateLogger<Ektepakt>();
        }

        /// <summary>
        /// Function entry point: unit basic information for the organization
        /// </summary>
        /// <param name="req">
        /// The HTTP request.
        /// </param>
        /// <param name="log">
        /// The logging object.
        /// </param>
        /// <param name="client">
        /// The HTTP client.
        /// </param>
        /// <returns>
        /// A <see cref="HttpResponseMessage"/>.
        /// </returns>
        [Function("Ektepakt")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);
            return await EvidenceSourceResponse.CreateResponse(req, () => GetValuesEktepakt(evidenceHarvesterRequest.SubjectParty.NorwegianSocialSecurityNumber));
        }

        private async Task<List<EvidenceValue>> GetValuesEktepakt(string identifier)
        {
            var url = _settings.EktepaktV2Uri + $"/api/v1/fnr/{identifier}?spraakkode=NOB";

            var response = await Requests.GetData<EktepaktV2>(_maskinportenClient, url, _logger);

            var mappedObject = MapEktepaktDD(response);

            var ecb = new EvidenceBuilder(_metadata, "Ektepakt");
            ecb.AddEvidenceValue("default", JsonConvert.SerializeObject(mappedObject), "Løsøreregisteret", false);
            return ecb.GetEvidenceValues();
        }

        private EktepaktResponse MapEktepaktDD(EktepaktV2 input)
        {
            var response = new EktepaktResponse()
            {
                Ektepakter = new List<EktepaktModel>()
            };

            foreach (var a in input.ektepakt)
            {
                var item = new EktepaktModel()
                {
                    SpouseName = a.rolle[1].person.navn.fornavn + " " + a.rolle[1].person.navn.etternavn,
                    EntryDate = a.innkomsttidspunkt
                };

                response.Ektepakter.Add(item);
            }

            return response;
        }

        public static EvidenceCode GetDefinitionEktepaktV2()
        {
            return new EvidenceCode()
            {
                EvidenceCodeName = "Ektepakt",
                BelongsToServiceContexts = new List<string>() { Constants.DD },
                Description = "List marriage settlements",
                DatasetAliases = new () { new () { DatasetAliasName = "EktepaktV2", ServiceContext = Constants.DD } },
                Values = new List<EvidenceValue>()
                {
                    new EvidenceValue()
                    {
                        EvidenceValueName = "default",
                        Source = Constants.SourceLosoreregisteret,
                        ValueType = EvidenceValueType.JsonSchema,
                        Description = $"Json payload from {Constants.SourceLosoreregisteret}"
                    }
                }
            };
        }
    }
}
