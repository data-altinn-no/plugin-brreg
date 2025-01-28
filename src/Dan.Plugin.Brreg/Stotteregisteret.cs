using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Dan.Common.Enums;
using Dan.Common.Interfaces;
using Dan.Common.Models;
using Dan.Common.Util;
using Dan.Plugin.Brreg.Config;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Constants = Dan.Plugin.Brreg.Models.Constants;

namespace Dan.Plugin.Brreg
{
    public class Stotteregisteret
    {
        private readonly ILogger _logger;
        private Settings _settings;
        private readonly IEvidenceSourceMetadata _metadata;

        public Stotteregisteret(IOptions<Settings> settings, IEvidenceSourceMetadata evidenceSourceMetadata, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Kunngjoringer>();
            _settings = settings.Value;
            _metadata = evidenceSourceMetadata;
        }

        [Function("StotteregisterLink")]
        public async Task<HttpResponseData> StotteregisterLink([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);
            return await EvidenceSourceResponse.CreateResponse(req, () => GetStotteregisterLink(evidenceHarvesterRequest.SubjectParty.NorwegianOrganizationNumber));
        }

        private async Task<List<EvidenceValue>> GetStotteregisterLink(string norwegianOrganizationNumber)
        {
            var url = string.Format(_settings.StotteregisterUrl, norwegianOrganizationNumber);

            var eb = new EvidenceBuilder(_metadata, nameof(Kunngjoringer));

            eb.AddEvidenceValue($"url", url);

            return eb.GetEvidenceValues();
        }

        public static EvidenceCode GetDefinition()
        {
            return new EvidenceCode()
            {
                Description = "Link to grants",
                EvidenceCodeName = "StotteregisteretUrl",
                BelongsToServiceContexts = new List<string>() { Constants.DIGOKFRIV },
                IsPublic = true,
                Values = new List<EvidenceValue>()
                {
                    new EvidenceValue()
                    {
                        EvidenceValueName = "url",
                        ValueType = EvidenceValueType.Uri,
                        Source = Constants.SourceStotteRegisteret
                    }
                }
            };
        }
    }
}
