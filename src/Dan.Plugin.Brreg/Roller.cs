using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NJsonSchema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dan.Common.Enums;
using Dan.Common.Exceptions;
using Dan.Common.Interfaces;
using Dan.Common.Models;
using Dan.Common.Util;
using Dan.Plugin.Brreg.Config;
using Dan.Plugin.Brreg.Helpers;
using Dan.Plugin.Brreg.Models;
using Dan.Plugin.Brreg.Models.Roller;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Options;

namespace Dan.Plugin.Brreg
{
    /// <summary>
    /// This class implements the Azure Function entry points for all the functions implemented by this evidence source. 
    /// </summary>   
    public class Roller
    {
        private readonly HttpClient _client;
        private Settings _settings;
        private IEvidenceSourceMetadata _metadata;
        private ILogger _logger;

        public Roller(IHttpClientFactory clientFactory, IOptions<Settings> settings, IEvidenceSourceMetadata metadata, ILoggerFactory loggerFactory)
        {
            _client = clientFactory.CreateClient("SafeHttpClient");
            _settings = settings.Value;
            _metadata = metadata;
            _logger = loggerFactory.CreateLogger<Roller>();
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
        /// <returns>
        /// A <see cref="HttpResponseMessage"/>.
        /// </returns>
        [Function("Roller")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, FunctionContext context)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);
            List<string> roleFilter = null;
            var roleFilterParameter = evidenceHarvesterRequest.Parameters?.FirstOrDefault();
            if (roleFilterParameter != null && roleFilterParameter.Value != null)
            {              
                roleFilter = roleFilterParameter.Value.ToString() == "*" // Include all roles
                    ? null 
                    : roleFilterParameter.Value.ToString()!.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();                
            }
            else
            {
                // We use a standard role filter to remove the non-obvious ones
                roleFilter = new List<string>
                {
                    "INNH", "DAGL", "REPR", "KONT", "LEDE", "NEST",
                    "MEDL", "VARA", "OBS", "DTSO", "DTPR", "REVI", "REGN"
                };
            }

            return await EvidenceSourceResponse.CreateResponse(req, () => GetRolesFromBrreg(evidenceHarvesterRequest.SubjectParty.NorwegianOrganizationNumber, roleFilter, _logger));
        }

        private async Task<List<EvidenceValue>> GetRolesFromBrreg(string organizationNumber, List<string> roleFilter, ILogger log)
        {
            string rawResult;
            try
            {
                var response = await _client.GetAsync($"{Requests.GetCcrUrlForMainUnit(organizationNumber)}/roller");
                if (response.StatusCode == HttpStatusCode.NotFound) {
                    throw new EvidenceSourcePermanentClientException(Constants.ERROR_ORGANIZATION_NOT_FOUND, $"{organizationNumber} was not found in the Central Coordinating Register for Legal Entities");
                }

                rawResult = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                throw new EvidenceSourcePermanentServerException(Constants.ERROR_CCR_UPSTREAM_ERROR, null, e);
            }

            var result = JsonConvert.DeserializeObject<RollerResponseBrreg>(rawResult);
            if (result == null)
            {
                throw new EvidenceSourcePermanentServerException(Constants.ERROR_CCR_UPSTREAM_ERROR, "Did not understand the data model returned from upstream source");
            }

            var eb = new EvidenceBuilder(_metadata, "Roller");
            eb.AddEvidenceValue("default", JsonConvert.SerializeObject(MapToInternal(result, roleFilter)));

            return eb.GetEvidenceValues();
        }

        private static RollerResponse MapToInternal(RollerResponseBrreg input, List<string> roleFilter)
        {
            var roleResponse = new RollerResponse { Roller = new List<Rolle>() };

            foreach (var roleGroup in input.rollegrupper)
            {
                foreach (var role in roleGroup.roller)
                {
                    if (roleFilter != null && !roleFilter.Contains(role.type.kode, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var mappedRole = new Rolle
                    {
                        Kode = role.type.kode,
                        Beskrivelse = role.type.beskrivelse,
                        Organisasjonsnummer = role.enhet?.organisasjonsnummer,
                        Fodselsdato = role.person?.fodselsdato,
                    };

                    if (role.enhet != null)
                    {
                        mappedRole.Navn = string.Join(' ', role.enhet.navn);
                    }
                    else if (role.person != null)
                    {
                        mappedRole.Navn = (role.person.navn.fornavn + " " + role.person.navn.mellomnavn + " " + role.person.navn.etternavn)
                            .Replace("  ", " ").Trim();
                    }

                    roleResponse.Roller.Add(mappedRole);
                }
            }

            return roleResponse;
        }

        /// <summary>
        /// Returns the evidence code definition
        /// </summary>
        /// <returns>The evidence code definition</returns>
        public static EvidenceCode GetDefinition()
        {
            return new EvidenceCode
            {
                EvidenceCodeName = "Roller",
                Description = "Enhetsregisteret",
                IsPublic = true,
                BelongsToServiceContexts = new List<string>() { Constants.EBEVIS, Constants.EDUEDILIGENCE, Constants.DIGOKFRIV },
                Values = new List<EvidenceValue>()
                {
                    new EvidenceValue
                    {
                        EvidenceValueName = "default",
                        ValueType = EvidenceValueType.JsonSchema,
                        JsonSchemaDefintion = JsonSchema.FromType<RollerResponse>().ToJson(Formatting.None),
                        Source = Constants.SourceEnhetsregisteret
                    }
                },
                Parameters = new List<EvidenceParameter>
                {
                    new EvidenceParameter
                    {
                        EvidenceParamName = "RolleFilter",
                        ParamType = EvidenceParamType.String,
                        Required = false
                    }
                }
            };
        }
    }
}
