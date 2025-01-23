using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Dan.Plugin.Brreg.Models;
using System.Threading.Tasks;
using Dan.Common;
using Dan.Common.Enums;
using Dan.Common.Exceptions;
using Dan.Common.Extensions;
using Dan.Common.Interfaces;
using Dan.Common.Models;
using Dan.Common.Util;
using Dan.Plugin.Brreg.Config;
using Dan.Plugin.Brreg.Helpers;
using Dan.Plugin.Brreg.Models.EktepaktV2;
using Dan.Plugin.Brreg.Models.LosoreV2;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Constants = Dan.Plugin.Brreg.Models.Constants;
using StackExchange.Redis;
using Azure;
using NJsonSchema;

namespace Dan.Plugin.Brreg
{


    public class Losore
    {
        private ILogger _logger;
        private readonly IEvidenceSourceMetadata _metadata;
        private Settings _settings;
        private HttpClient _maskinportenClient;

        public Losore(IHttpClientFactory clientFactory, IOptions<Settings> settings, IEvidenceSourceMetadata metadata, ILoggerFactory loggerFactory)
        {
            _metadata = metadata;
            _settings = settings.Value;
            _logger = loggerFactory.CreateLogger<Losore>();
            _maskinportenClient = clientFactory.CreateClient("myMaskinportenClient");
        }

        [Function("RettsstiftelserVirksomhet")]
        public async Task<HttpResponseData> GetRettsstiftelserVirksomhet([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);
            return await EvidenceSourceResponse.CreateResponse(req,
                () => GetRettsstiftelserValuesVirksomhet(evidenceHarvesterRequest.SubjectParty.NorwegianOrganizationNumber));
        }

        private async Task<List<EvidenceValue>> GetRettsstiftelserValuesVirksomhet(string norwegianOrganizationNumber)
        {
            var url = _settings.LosoreURI + $"/api/v2/rettsstiftelse/orgnr/{norwegianOrganizationNumber}";

            var response = await Requests.GetData<LosoreV2>(_maskinportenClient, url, _logger);

            var ecb = new EvidenceBuilder(_metadata, "RettsstiftelserVirksomhet");
            ecb.AddEvidenceValue("default", JsonConvert.SerializeObject(response), "Løsøreregisteret", false);
            return ecb.GetEvidenceValues();
        }

        [Function("RettsstiftelserKjoretoy")]
        public async Task<HttpResponseData> GetRettsstiftelserKjoretoy([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);
            
            return await EvidenceSourceResponse.CreateResponse(req,
                () => GetRettsstiftelserValuesKjoretoy(evidenceHarvesterRequest));
        }

        private async Task<List<EvidenceValue>> GetRettsstiftelserValuesKjoretoy(EvidenceHarvesterRequest ehr)
        {
            if (!ehr.TryGetParameter("Registreringsnummer", out string regnr))
            {
                throw new EvidenceSourcePermanentClientException(Constants.ERROR_PARAMETERS_MISSING, "Missing registration number");
            }

            var url = _settings.LosoreURI + $"/api/v2/rettsstiftelse/regnr/{regnr}";
            var response = await Requests.GetData<LosoreV2>(_maskinportenClient, url, _logger);

            var ecb = new EvidenceBuilder(_metadata, "RettsstiftelserKjoretoy");
            ecb.AddEvidenceValue("default", JsonConvert.SerializeObject(response), "Løsøreregisteret", false);
            return ecb.GetEvidenceValues();
        }

        [Function("RettsstiftelserPerson")]
        public async Task<HttpResponseData> GetRettsstiftelserPerson([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);
            return await EvidenceSourceResponse.CreateResponse(req,
                () => GetRettsstiftelserValuesPerson(evidenceHarvesterRequest.SubjectParty.NorwegianSocialSecurityNumber));
        }

        private async Task<List<EvidenceValue>> GetRettsstiftelserValuesPerson(string norwegianSocialSecurityNumber)
        {
            var url = _settings.LosoreURI + $"/api/v2/rettsstiftelse/fnr/{norwegianSocialSecurityNumber}";
            var response = await Requests.GetData<LosoreV2>(_maskinportenClient, url, _logger);

            var ecb = new EvidenceBuilder(_metadata, "RettsstiftelserKjoretoy");
            ecb.AddEvidenceValue("default", JsonConvert.SerializeObject(response), "Løsøreregisteret", false);
            return ecb.GetEvidenceValues();
        }

        public static EvidenceCode GetDefinitionRettsstiftelserKjoretoy()
        {
            return new EvidenceCode()
            {
                EvidenceCodeName = "RettsstiftelserKjoretoy",
                BelongsToServiceContexts = new List<string>() { Constants.EDUEDILIGENCE },
                Description = "Henter rettsstiftelser om kjøretøy - alle parter på authenticationrequest må settes til spørrende organisasjonsnummer",
                EvidenceSource = "Brreg",
                Values = new List<EvidenceValue>()
                {
                    new EvidenceValue()
                    {
                        EvidenceValueName = "default",
                        Source = Constants.SourceLosoreregisteret,
                        ValueType = EvidenceValueType.JsonSchema,
                        Description = $"Json payload from {Constants.SourceLosoreregisteret}",
                        JsonSchemaDefintion =  JsonSchema.FromType<LosoreV2>().ToJson(Formatting.None),
                    }
                },
                Parameters = new List<EvidenceParameter>()
                {
                    new EvidenceParameter()
                    {
                        EvidenceParamName = "Registreringsnummer",
                        ParamType = EvidenceParamType.String,
                        Required = true
                    }
                },
                AuthorizationRequirements = new List<Requirement>()
                {
                    new PartyTypeRequirement()
                    {
                        AllowedPartyTypes = new AllowedPartyTypesList()
                        {
                            new KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>(AccreditationPartyTypes.Subject,
                                PartyTypeConstraint.PrivateEnterprise)
                        }
                    },
                    new AccreditationPartyRequirement()
                    {
                        PartyRequirements = new List<AccreditationPartyRequirementType>()
                        {
                            AccreditationPartyRequirementType.RequestorAndOwnerAreEqual, AccreditationPartyRequirementType.RequestorAndSubjectAreEqual, AccreditationPartyRequirementType.SubjectAndOwnerAreEqual
                        }
                    }
                }
            };

        }

        public static EvidenceCode GetDefinitionRettsstiftelserVirksomhet()
        {
            return new EvidenceCode()
            {
                EvidenceCodeName = "RettsstiftelserVirksomhet",
                BelongsToServiceContexts = new List<string>() { Constants.EDUEDILIGENCE },
                Description = "",
                EvidenceSource = "Brreg",
                Values = new List<EvidenceValue>()
                {
                    new EvidenceValue()
                    {
                        EvidenceValueName = "default",
                        Source = Constants.SourceLosoreregisteret,
                        ValueType = EvidenceValueType.JsonSchema,
                        Description = $"Json payload from {Constants.SourceLosoreregisteret}",
                        JsonSchemaDefintion = JsonSchema.FromType<LosoreV2>().ToJson(Formatting.None)
                    }
                },
                AuthorizationRequirements = new List<Requirement>()
                {
                    new PartyTypeRequirement()
                    {
                        AllowedPartyTypes = new AllowedPartyTypesList()
                        {
                            new KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>(AccreditationPartyTypes.Subject,
                                PartyTypeConstraint.PrivateEnterprise)
                        }
                    }
                }
            };
        }

        public static EvidenceCode GetDefinitionRettsstiftelserPerson()
        {
            return new EvidenceCode()
            {
                EvidenceCodeName = "RettsstiftelserPerson",
                BelongsToServiceContexts = new List<string>() { Constants.EDUEDILIGENCE },
                Description = "",
                Values = new List<EvidenceValue>()
                {
                    new EvidenceValue()
                    {
                        EvidenceValueName = "default",
                        Source = Constants.SourceLosoreregisteret,
                        ValueType = EvidenceValueType.JsonSchema,
                        Description = $"Json payload from {Constants.SourceLosoreregisteret}",
                        JsonSchemaDefintion = JsonSchema.FromType<LosoreV2>().ToJson(Formatting.None)
                    }
                },
                AuthorizationRequirements = new List<Requirement>()
                {
                    new PartyTypeRequirement()
                    {
                        AllowedPartyTypes = new AllowedPartyTypesList()
                        {
                            new KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>(AccreditationPartyTypes.Subject, PartyTypeConstraint.PrivatePerson)
                        }
                    }
                }
            };
        }
    }
}
