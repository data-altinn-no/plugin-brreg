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
using NJsonSchema;


namespace Nadobe.EvidenceSources.ES_BR
{

    /// <summary>
    /// This class implements the Azure Function entry points for all the functions implemented by this evidence source. 
    /// </summary>   
    public class Ektepakt
    {
        private HttpClient _client;
        private Settings _settings;
        private readonly IEvidenceSourceMetadata _metadata;
        private readonly ILogger _logger;

        public Ektepakt(IHttpClientFactory clientFactory, IOptions<Settings> settings, IEvidenceSourceMetadata evidenceSourceMetadata, ILoggerFactory loggerFactory)
        {
            _client = clientFactory.CreateClient("SafeHttpClient");
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
            var evidenceHarvesterRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);
            return await EvidenceSourceResponse.CreateResponse(req, () => GetEktepaktFromBR(evidenceHarvesterRequest.SubjectParty, log));
        }

        private async Task<List<EvidenceValue>> GetEktepaktFromBR(Party? party, ILogger log)
        {
            var binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var rootDirectory = Path.GetFullPath(Path.Combine(binDirectory, ".."));

            string payload = File.ReadAllText("Config\\EktepaktTemplate.xml");
            payload = payload.Replace("ektepaktUsername", _settings.EktepaktUserName);
            payload = payload.Replace("ektepaktPassword", _settings.EktepaktPassword);
            payload = payload.Replace("ssn", party.NorwegianSocialSecurityNumber);

            var ecb = new EvidenceBuilder(_metadata, "Ektepakt");

            try
            {
                var response = await _client.PostAsync($"{_settings.EktepaktUri}/losore/heftelser/LosoreOnlineService", new StringContent(payload, Encoding.UTF8, "text/xml"));
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception($"Ektepakt error: Status code != 200, status = {response.StatusCode}");
                }
                var mapped = await MapToInternal(response, party.NorwegianSocialSecurityNumber);
                ecb.AddEvidenceValue("Default", JsonConvert.SerializeObject(mapped),Constants.SourceLosoreregisteret, false);
                return ecb.GetEvidenceValues();

            } catch (Exception ex)
            {
                log.LogError($"Ektepakt error {ex.Message}");
                throw new EvidenceSourceTransientException(Constants.ERROR_NO_REPORT_AVAILABLE, $"Error looking up ektepakt for {party.GetAsString()}");
            }
        }

        private async Task<EktepaktResponse> MapToInternal(HttpResponseMessage response, string ssn)
        {
            EktepaktResponse ektepaktResponse = new EktepaktResponse() { Ektepakter = new List<EktepaktModel>() };

            XElement soapResponse = XElement.Load(await response.Content.ReadAsStreamAsync());
            string xmlResponse = soapResponse?.Descendants("return").FirstOrDefault()?.Value;
            if (xmlResponse == null)
            {
                throw new Exception($"No xml data returned looking up ektepakt for {ssn}");
            }

            XElement returnXml = XElement.Parse(xmlResponse);
            foreach (XElement dagbok in returnXml.Descendants("Dagbok_Kortv"))
                if (dagbok.Elements("doktype").FirstOrDefault()?.Value == "EP")
                {
                    DateTime? date = null;

                    if (dagbok.Elements("dgbdato").FirstOrDefault() != null)
                    {
                        date = DateTime.ParseExact(dagbok.Elements("dgbdato").FirstOrDefault()?.Value, "dd.MM.yyyy", CultureInfo.InvariantCulture);
                    }

                    ektepaktResponse.Ektepakter.Add(new EktepaktModel()
                    {
                        Date = date,
                        SpouseName = dagbok.Elements("kreditor").FirstOrDefault()?.Value
                    });
                }

            return ektepaktResponse;
        }

        /// <summary>
        /// Returns the evidence code definition
        /// </summary>
        /// <returns>The evidence code definition</returns>
        public static EvidenceCode GetDefinition()
        {
            return new EvidenceCode()
            {
                EvidenceCodeName = "Ektepakt",
                Description = "Løseøreregisteret",
                BelongsToServiceContexts = new List<string>() { "OED" },
                Values = new List<EvidenceValue>
                {
                    new EvidenceValue()
                    {
                        EvidenceValueName = "Default",
                        ValueType = EvidenceValueType.JsonSchema,
                        JsonSchemaDefintion = JsonSchema.FromType<EktepaktResponse>().ToJson(Formatting.Indented),
                        Source = Constants.SourceLosoreregisteret
                    }
                }               
            };
        }       
    }
}
