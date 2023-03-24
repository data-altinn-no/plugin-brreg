using Dan.Common.Enums;
using Dan.Common.Exceptions;
using Dan.Common.Interfaces;
using Dan.Common.Models;
using Dan.Common.Util;
using Dan.Plugin.Brreg.Config;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Options;

namespace Nadobe.EvidenceSources.ES_BR
{
    using Dan.Plugin.Brreg.Models;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// This class implements the Azure Function entry points for all the functions implemented by this evidence source. 
    /// </summary>   
    public class Konkurs
    {
        public HttpClient _client;
        private ILogger _logger;
        private readonly IEvidenceSourceMetadata _metadata;
        private Settings _settings;

        public Konkurs(IHttpClientFactory clientFactory, IOptions<Settings> settings, IEvidenceSourceMetadata metadata)
        {
            _client = clientFactory.CreateClient("SafeHttpClient");
            _metadata = metadata;
            _settings = settings.Value;
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
        [Function("KonkursDrosje")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequestData req, FunctionContext context)
        {
            _logger = context.GetLogger(context.FunctionDefinition.Name);

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);
            return await EvidenceSourceResponse.CreateResponse(req, ()=> GetUnitBasicInformationFromBrreg(evidenceHarvesterRequest.SubjectParty.NorwegianOrganizationNumber, _client));
        }

        /// <summary>
        /// Returns the evidence code definition
        /// </summary>
        /// <returns>The evidence code definition</returns>
        public static EvidenceCode GetDefinition()
        {
            return new EvidenceCode()
            {
                EvidenceCodeName = "KonkursDrosje",
                Description = "Informasjon om en virksomhets konkurstilstand",
                BelongsToServiceContexts = new List<string>() { "Drosjeloyve" },
                Values = new List<EvidenceValue>
                {
                    new EvidenceValue()
                    {
                        EvidenceValueName = "Organisasjonsnummer",
                        ValueType = EvidenceValueType.Number,
                        Source = Constants.SourceEnhetsregisteret
                    },
                    new EvidenceValue()
                    {
                        EvidenceValueName = "Organisasjonsnavn",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },
                    new EvidenceValue()
                    {
                        EvidenceValueName = "Adresse",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },
                    new EvidenceValue()
                    {
                        EvidenceValueName = "Postnummer",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },
                    new EvidenceValue()
                    {
                        EvidenceValueName = "Poststed",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },
                    new EvidenceValue()
                    {
                        EvidenceValueName = "Land",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },
                    new EvidenceValue()
                    {
                        EvidenceValueName = "Forretningsadresse",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },
                    new EvidenceValue()
                    {
                        EvidenceValueName = "ForretningsadressePoststed",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },
                    new EvidenceValue()
                    {
                        EvidenceValueName = "ForretningsadressePostnummer",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },
                    new EvidenceValue()
                    {
                        EvidenceValueName = "ForretningsadresseLand",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },
                    new EvidenceValue()
                    {
                        EvidenceValueName = "UnderAvvikling",
                        ValueType = EvidenceValueType.Boolean,
                        Source = Constants.SourceEnhetsregisteret
                    },

                    new EvidenceValue()
                    {
                        EvidenceValueName = "Konkurs",
                        ValueType = EvidenceValueType.Boolean,
                        Source = Constants.SourceEnhetsregisteret
                    },

                    new EvidenceValue()
                    {
                        EvidenceValueName = "UnderTvangsavviklingEllerTvangsopplosning",
                        ValueType = EvidenceValueType.Boolean,
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

        private async Task<List<EvidenceValue>> GetUnitBasicInformationFromBrreg(string organization, HttpClient client)
        {
            dynamic result = await HarvestFromBrreg(organization, client);


            bool? isBeingDissolved = null;
            bool? isUnderBankruptcy = null;
            bool? isBeingForciblyDissolved = null;
            string businessAddressStreet = string.Empty;
            string businessAddressZip = string.Empty;
            string businessAddressCity = string.Empty;
            string postalAddressStreet = string.Empty;
            string postalAddressZip = string.Empty;
            string postalAddressCity = string.Empty;
            string postalAddressCountryCode = string.Empty;
            string businessAddressCountry = string.Empty;


            long organizationNumber = result["organisasjonsnummer"];
            string organizationName = result["navn"];

            if(result["forretningsadresse"] != null)
            {
                if (result["forretningsadresse"]["adresse"] != null)
                {
                    businessAddressStreet = string.Join(", ", result["forretningsadresse"]["adresse"]);
                }

                if (result["forretningsadresse"]["postnummer"] != null)
                {
                    businessAddressZip = result["forretningsadresse"]["postnummer"];
                }

                if (result["forretningsadresse"]["poststed"] != null)
                {
                    businessAddressCity = result["forretningsadresse"]["poststed"];
                }
                
                if (result["forretningsadresse"]["land"] != null)
                {
                    businessAddressCountry = result["forretningsadresse"]["land"];
                }
            }

            if (result["postadresse"] != null)
            {
                if (result["postadresse"]["adresse"] != null)
                {
                    postalAddressStreet = string.Join(", ", result["postadresse"]["adresse"]);
                }

                if (result["postadresse"]["postnummer"] != null)
                {
                    postalAddressZip = result["postadresse"]["postnummer"];
                }

                if (result["postadresse"]["poststed"] != null)
                {
                    postalAddressCity = result["postadresse"]["poststed"];
                }

                if (result["postadresse"]["land"] != null)
                {
                    postalAddressCountryCode = result["postadresse"]["land"];
                }
            }

            if (result["underAvvikling"] != null)
            {
                isBeingDissolved = (bool)result["underAvvikling"];
            }

            if (result["konkurs"] != null)
            {
                isUnderBankruptcy = (bool)result["konkurs"];
            }

            if (result["underTvangsavviklingEllerTvangsopplosning"] != null)
            {
                isBeingForciblyDissolved = (bool)result["underTvangsavviklingEllerTvangsopplosning"];
            }

            var ecb = new EvidenceBuilder(_metadata, "KonkursDrosje");
            ecb.AddEvidenceValue("Organisasjonsnummer", organizationNumber);
            ecb.AddEvidenceValue("Organisasjonsnavn", organizationName);
            ecb.AddEvidenceValue("Adresse", postalAddressStreet);
            ecb.AddEvidenceValue("Postnummer", postalAddressZip);
            ecb.AddEvidenceValue("Poststed", postalAddressCity);
            ecb.AddEvidenceValue("Land", postalAddressCountryCode);

            ecb.AddEvidenceValue("Forretningsadresse", businessAddressStreet);
            ecb.AddEvidenceValue("ForretningsadressePostnummer", businessAddressZip);
            ecb.AddEvidenceValue("ForretningsadressePoststed", businessAddressCity);
            ecb.AddEvidenceValue("ForretningsadresseLand", businessAddressCountry);

            if (isBeingDissolved != null)
            {
                ecb.AddEvidenceValue("UnderAvvikling", isBeingDissolved.Value);
            }

            if (isUnderBankruptcy != null)
            {
                ecb.AddEvidenceValue("Konkurs", isUnderBankruptcy.Value);
            }

            if (isBeingForciblyDissolved != null)
            {
                ecb.AddEvidenceValue("UnderTvangsavviklingEllerTvangsopplosning", isBeingForciblyDissolved.Value);
            }

            return ecb.GetEvidenceValues();
        }

        private static async Task<dynamic> HarvestFromBrreg(string organization, HttpClient client)
        {
            string rawResult;
            try
            {
                var response = await client.GetAsync($"http://data.brreg.no/enhetsregisteret/api/enheter/{organization}");
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    response = await client.GetAsync($"http://data.brreg.no/enhetsregisteret/api/underenheter/{organization}");
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new EvidenceSourcePermanentClientException(Constants.ERROR_ORGANIZATION_NOT_FOUND, $"{organization} was not found in the Central Coordinating Register for Legal Entities");
                    }
                }

                rawResult = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                throw new EvidenceSourcePermanentServerException(Constants.ERROR_CCR_UPSTREAM_ERROR, null, e);
            }

            dynamic result = JsonConvert.DeserializeObject(rawResult);
            if (result == null || result["organisasjonsnummer"] == null)
            {
                throw new EvidenceSourcePermanentServerException(Constants.ERROR_CCR_UPSTREAM_ERROR, "Did not understand the data model returned from upstream source");
            }

            return result;
        }
    }
}
