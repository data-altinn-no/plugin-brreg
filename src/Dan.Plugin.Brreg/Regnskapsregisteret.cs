using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Dan.Common.Enums;
using Dan.Common.Extensions;
using Dan.Common.Interfaces;
using Dan.Common.Models;
using Dan.Common.Util;
using Microsoft.Extensions.Options;
using Dan.Plugin.Brreg.Config;
using Dan.Plugin.Brreg.Helpers;
using Dan.Plugin.Brreg.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace ES_BR
{
    public class Regnskapsregisteret
    {
        private HttpClient _client;
        private Settings _settings;
        private ILogger _logger;
        private IEvidenceSourceMetadata _metadata;

        public Regnskapsregisteret(IHttpClientFactory clientFactory, IOptions<Settings> settings, IEvidenceSourceMetadata metadata)
        {
            _client = clientFactory.CreateClient("SafeHttpClient");
            _settings = settings.Value;
            _client.BaseAddress = new Uri(_settings.RegnskapsregisteretUri);
            _metadata = metadata;
        }

        [Function("Regnskapsregisteret")]
        public async Task<HttpResponseData> RRAccounts([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, FunctionContext context)
        {
            _logger = context.GetLogger(context.FunctionDefinition.Name);
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);
            evidenceHarvesterRequest.TryGetParameter("Aar", out int aar);
            evidenceHarvesterRequest.TryGetParameter("Type", out string type);
            return await EvidenceSourceResponse.CreateResponse(req, () => GetRegnskap(evidenceHarvesterRequest.SubjectParty.NorwegianOrganizationNumber, aar, type));
        }

        [Function("RegnskapsregisteretOpen")]
        public async Task<HttpResponseData> RRAccountsOpen([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, FunctionContext context)
        {
            _logger = context.GetLogger(context.FunctionDefinition.Name);
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);
            evidenceHarvesterRequest.TryGetParameter("Aar", out int aar);
            evidenceHarvesterRequest.TryGetParameter("Type", out string type);
            return await EvidenceSourceResponse.CreateResponse(req, () => GetRegnskap(evidenceHarvesterRequest.SubjectParty.NorwegianOrganizationNumber, aar, type));
        }

        [Function("RegnskapsregisteretId")]
        public async Task<HttpResponseData> RRById([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, FunctionContext context)
        {
            _logger = context.GetLogger(context.FunctionDefinition.Name);
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);
            evidenceHarvesterRequest.TryGetParameter("Id", out string id);
            return await EvidenceSourceResponse.CreateResponse(req, () => GetRegnskapId(evidenceHarvesterRequest.SubjectParty.NorwegianOrganizationNumber, id));
        }

        private async Task<List<EvidenceValue>> GetRegnskap(string orgno, int year, string type = "SELSKAP")
        {
            string url = $"{_settings.RegnskapsregisteretUri}/regnskapsregisteret/regnskap/{orgno}?%C3%A5={year}&regnskapstype={type.ToUpper()}";

            var result = await Requests.MakeRequest(url, _client, _settings.RegnskapsregisteretUsername, _settings.RegnskapsregisteretPw, HttpMethod.Get, _logger);

            var ecb = new EvidenceBuilder(_metadata, "Regnskapsregisteret");
            ecb.AddEvidenceValue("default", JsonConvert.SerializeObject(result), Constants.SourceRegnskapsregisteret, false);
            return ecb.GetEvidenceValues();
        }

        private async Task<List<EvidenceValue>> GetRegnskapId(string orgno, string id)
        {
            string url = $"{_settings.RegnskapsregisteretUri}/regnskapsregisteret/regnskap/{orgno}/{id}";

            var result = await Requests.MakeRequest(url, _client, _settings.RegnskapsregisteretUsername, _settings.RegnskapsregisteretPw, HttpMethod.Get, _logger);

            var ecb = new EvidenceBuilder(_metadata, "Regnskapsregisteret");
            ecb.AddEvidenceValue("default", JsonConvert.SerializeObject(result), Constants.SourceRegnskapsregisteret, false);
            return ecb.GetEvidenceValues();
        }

        public static EvidenceCode GetDefinitionRegnskap()
        {

            return new EvidenceCode()
            {
                EvidenceCodeName = "Regnskapsregisteret",
                Description = "The public accounts of an organization",
                BelongsToServiceContexts = new List<string>() { Constants.EDUEDILIGENCE, Constants.EBEVIS },
                Values = new List<EvidenceValue>
                {
                    new EvidenceValue()
                    {
                        EvidenceValueName = "default",
                        ValueType = EvidenceValueType.JsonSchema,
                        Source = Constants.SourceRegnskapsregisteret
                    }
                },
                AuthorizationRequirements = new List<Requirement>()
                {
                    new PartyTypeRequirement()
                    {
                        AppliesToServiceContext = new List<string>() { Constants.EDUEDILIGENCE, Constants.EBEVIS},
                        AllowedPartyTypes = new AllowedPartyTypesList()
                        {
                            new KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>(AccreditationPartyTypes.Requestor,PartyTypeConstraint.PublicAgency),
                            new KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>(AccreditationPartyTypes.Subject, PartyTypeConstraint.PrivateEnterprise)
                        }
                    }
                },
                Parameters = new List<EvidenceParameter>()
                {
                    new EvidenceParameter() { EvidenceParamName = "Aar", ParamType = EvidenceParamType.Number, Required = true },
                    new EvidenceParameter() { EvidenceParamName = "Type", ParamType = EvidenceParamType.String, Required = true },
                }
            };
        }

        public static EvidenceCode GetDefinitionRegnskapId()
        {

            return new EvidenceCode()
            {
                EvidenceCodeName = "RegnskapsregisteretId",
                Description = "The specified public accounts of an organization",
                BelongsToServiceContexts = new List<string>() { Constants.EDUEDILIGENCE, Constants.EBEVIS },
                Values = new List<EvidenceValue>
                {
                    new EvidenceValue()
                    {
                        EvidenceValueName = "default",
                        ValueType = EvidenceValueType.JsonSchema,
                        Source = Constants.SourceRegnskapsregisteret
                    }
                },
                AuthorizationRequirements = new List<Requirement>()
                {
                    new PartyTypeRequirement()
                    {
                        AllowedPartyTypes = new AllowedPartyTypesList()
                        {
                            new KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>(AccreditationPartyTypes.Requestor,PartyTypeConstraint.PublicAgency),
                            new KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>(AccreditationPartyTypes.Subject, PartyTypeConstraint.PrivateEnterprise)
                        }
                    }
                },
                Parameters = new List<EvidenceParameter>()
                {
                    new EvidenceParameter() { EvidenceParamName = "Id", ParamType = EvidenceParamType.Number, Required = true }
                }
            };
        }

        public static EvidenceCode GetDefinitionRegnskapOpen()
        {

            return new EvidenceCode()
            {
                EvidenceCodeName = "RegnskapsregisteretOpen",
                Description = "The public accounts of an organization",
                BelongsToServiceContexts = new List<string>() { Constants.DIGOKFRIV },
                IsPublic = true,
                Values = new List<EvidenceValue>
                {
                    new EvidenceValue()
                    {
                        EvidenceValueName = "default",
                        ValueType = EvidenceValueType.JsonSchema,
                        Source = Constants.SourceRegnskapsregisteret
                    }
                },                
                Parameters = new List<EvidenceParameter>()
                {
                    new EvidenceParameter() { EvidenceParamName = "Aar", ParamType = EvidenceParamType.Number, Required = true },
                    new EvidenceParameter() { EvidenceParamName = "Type", ParamType = EvidenceParamType.String, Required = true },
                }
            };
        }
    }
}
