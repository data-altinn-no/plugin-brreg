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
    public class Kunngjoringer
    {
        private readonly ILogger _logger;
        private Settings _settings;
        private readonly IEvidenceSourceMetadata _metadata;

        public Kunngjoringer(IOptions<Settings> settings, IEvidenceSourceMetadata evidenceSourceMetadata, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Kunngjoringer>();
            _settings = settings.Value;
            _metadata = evidenceSourceMetadata;
        }

        [Function("Kunngjoringer")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function,  "post")] HttpRequestData req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);
            return await EvidenceSourceResponse.CreateResponse(req, () => GetAnnouncementUrl(evidenceHarvesterRequest.SubjectParty.NorwegianOrganizationNumber));
        }

        private async Task<List<EvidenceValue>> GetAnnouncementUrl(string norwegianOrganizationNumber)
        {
            var url = string.Format(_settings.AnnouncementUrl, norwegianOrganizationNumber);

            var eb = new EvidenceBuilder(_metadata, nameof(Kunngjoringer));

            eb.AddEvidenceValue($"Url", url);

            return eb.GetEvidenceValues();
        }

        public static EvidenceCode GetDefinition()
        {
            return new EvidenceCode()
            {
                Description = "Link to announcements regarding an enterprise",
                EvidenceCodeName = "Kunngjoringer",
                BelongsToServiceContexts = new List<string>() { Constants.EDUEDILIGENCE, Constants.DIGOKFRIV },
                IsPublic = true,
                Values = new List<EvidenceValue>()
                {
                    new EvidenceValue()
                    {
                        EvidenceValueName = "Url",
                        ValueType = EvidenceValueType.Uri,
                        Source = Constants.SourceEnhetsregisteret
                    }
                }
            };
        }
    }
}
