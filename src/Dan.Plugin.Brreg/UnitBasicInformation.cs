using Dan.Common.Enums;
using Dan.Common.Exceptions;
using Dan.Common.Interfaces;
using Dan.Common.Models;
using Dan.Common.Util;
using Dan.Plugin.Brreg.Config;
using Dan.Plugin.Brreg.Helpers;
using Dan.Plugin.Brreg.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Options;

namespace Nadobe.EvidenceSources.ES_BR
{
    using Google.Protobuf;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// This class implements the Azure Function entry points for all the functions implemented by this evidence source. 
    /// </summary>   
    public class UnitBasicInformation
    {
        public HttpClient _client;
        private ILogger _logger;
        private IEvidenceSourceMetadata _metadata;
        private Settings _settings;

        public UnitBasicInformation(IHttpClientFactory clientFactory, IOptions<Settings> settings, IEvidenceSourceMetadata metadata)
        {
            _client = clientFactory.CreateClient("SafeHttpClient");
            _settings = settings.Value;
            _metadata = metadata;
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
        [Function("UnitBasicInformation")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequestData req, FunctionContext context)
        {
            _logger = context.GetLogger(context.FunctionDefinition.Name);
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);
            return await EvidenceSourceResponse.CreateResponse(req, () => GetUnitBasicInformationFromBrreg(evidenceHarvesterRequest.SubjectParty.NorwegianOrganizationNumber, _client));

        }

        /// <summary>
        /// Returns the evidence code definition
        /// </summary>
        /// <returns>The evidence code definition</returns>
        public static EvidenceCode GetDefinition()
        {
            return new EvidenceCode()
            {
                EvidenceCodeName = "UnitBasicInformation",
                Description = "Return units basic information for the subject company",
                BelongsToServiceContexts = new List<string>() { Constants.EBEVIS, Constants.EDUEDILIGENCE, Constants.SERIOSITET, Constants.DIGOKFRIV },
                IsPublic = true,
                Values = new List<EvidenceValue>
                {
                    new EvidenceValue()
                    {
                        EvidenceValueName = "OrganizationNumber",
                        ValueType = EvidenceValueType.Number,
                        Source = Constants.SourceEnhetsregisteret
                    },

                    new EvidenceValue()
                    {
                        EvidenceValueName = "OrganizationName",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },

                    new EvidenceValue()
                    {
                        EvidenceValueName = "OrganizationForm",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },

                    new EvidenceValue()
                    {
                        EvidenceValueName = "IndustryCode1",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },

                    new EvidenceValue()
                    {
                        EvidenceValueName = "IndustryCode1Description",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },

                    new EvidenceValue()
                    {
                        EvidenceValueName = "IndustryCode2",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },

                    new EvidenceValue()
                    {
                        EvidenceValueName = "IndustryCode2Description",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },

                    new EvidenceValue()
                    {
                        EvidenceValueName = "IndustryCode3",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },

                    new EvidenceValue()
                    {
                        EvidenceValueName = "IndustryCode3Description",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },

                    new EvidenceValue()
                    {
                        EvidenceValueName = "BusinessAddressStreet",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },

                    new EvidenceValue()
                    {
                        EvidenceValueName = "BusinessAddressZip",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },

                    new EvidenceValue()
                    {
                        EvidenceValueName = "BusinessAddressCity",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },

                    new EvidenceValue()
                    {
                        EvidenceValueName = "PostalAddressStreet",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },

                    new EvidenceValue()
                    {
                        EvidenceValueName = "PostalAddressZip",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },

                    new EvidenceValue()
                    {
                        EvidenceValueName = "PostalAddressCity",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },

                    new EvidenceValue()
                    {
                        EvidenceValueName = "PostalAddressCountryCode",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },

                    new EvidenceValue()
                    {
                        EvidenceValueName = "CreatedInCentralRegisterForLegalEntities",
                        ValueType = EvidenceValueType.DateTime,
                        Source = Constants.SourceEnhetsregisteret
                    },

                    new EvidenceValue()
                    {
                        EvidenceValueName = "Established",
                        ValueType = EvidenceValueType.DateTime,
                        Source = Constants.SourceEnhetsregisteret
                    },

                    new EvidenceValue()
                    {
                        EvidenceValueName = "IsInRegisterOfBusinessEnterprises",
                        ValueType = EvidenceValueType.Boolean,
                        Source = Constants.SourceEnhetsregisteret
                    },

                    new EvidenceValue()
                    {
                        EvidenceValueName = "IsInValueAddedTaxRegister",
                        ValueType = EvidenceValueType.Boolean,
                        Source = Constants.SourceEnhetsregisteret
                    },

                    new EvidenceValue()
                    {
                        EvidenceValueName = "LatestFinacialStatement",
                        ValueType = EvidenceValueType.Number,
                        Source = Constants.SourceEnhetsregisteret
                    },

                    new EvidenceValue()
                    {
                        EvidenceValueName = "NumberOfEmployees",
                        ValueType = EvidenceValueType.Number,
                        Source = Constants.SourceEnhetsregisteret
                    },

                    new EvidenceValue()
                    {
                        EvidenceValueName = "IsBeingDissolved",
                        ValueType = EvidenceValueType.Boolean,
                        Source = Constants.SourceEnhetsregisteret
                    },

                    new EvidenceValue()
                    {
                        EvidenceValueName = "IsUnderBankruptcy",
                        ValueType = EvidenceValueType.Boolean,
                        Source = Constants.SourceEnhetsregisteret
                    },

                    new EvidenceValue()
                    {
                        EvidenceValueName = "IsBeingForciblyDissolved",
                        ValueType = EvidenceValueType.Boolean,
                        Source = Constants.SourceEnhetsregisteret
                    },
                    new EvidenceValue()
                    {
                        EvidenceValueName = "IsInRegistryOfNonProfitOrganizations",
                        ValueType = EvidenceValueType.Boolean,
                        Source = Constants.SourceEnhetsregisteret
                    },
                    new EvidenceValue()
                    {
                        EvidenceValueName = "CreatedInNonProfitRegistry",
                        ValueType = EvidenceValueType.DateTime,
                        Source = Constants.SourceEnhetsregisteret
                    },
                    new EvidenceValue()
                    {
                        EvidenceValueName = "HomePage",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },
                    new EvidenceValue()
                    {
                        EvidenceValueName = "Email",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },
                    new EvidenceValue()
                    {
                        EvidenceValueName = "SectorCode",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },
                    new EvidenceValue()
                    {
                        EvidenceValueName = "SectorCodeDescription",
                        ValueType = EvidenceValueType.String,
                        Source = Constants.SourceEnhetsregisteret
                    },
                    new EvidenceValue()
                    {
                        EvidenceValueName = "Activity",
                        ValueType = EvidenceValueType.String,
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
            dynamic result;

            if (organization == "910361279") // KJØRSVIKBUGEN OG MESNALI REGNSKAP - ENK i TT02
            {
                organization = "983175155"; // LANGFORS DIESEL MEDIA
                result = await HarvestFromBrreg(organization, client);
                result["navn"] = "KJØRSVIKBUGEN OG MESNALI REGNSKAP";
                result["organisasjonsnummer"] = 910361279;

            }
            else
            {
                result = await HarvestFromBrreg(organization, client);
            }

            string industryCode1 = string.Empty;
            string industryCode1Description = string.Empty;
            string industryCode2 = string.Empty;
            string industryCode2Description = string.Empty;
            string industryCode3 = string.Empty;
            string industryCode3Description = string.Empty;
            string postalAddressZip = string.Empty;
            string postalAddressCity = string.Empty;
            string postalAddressCountryCode = string.Empty;
            string postalAddressStreet = string.Empty;
            string businessAddressStreet = string.Empty;
            string businessAddressZip = string.Empty;
            string businessAddressCity = string.Empty;
            DateTime? createdInCentralRegisterForLegalEntities = null;
            DateTime? established = null;
            DateTime? createdInNonProfitRegistry = null;
            bool? isInRegisterOfBusinessEnterprises = null;
            bool? isInValueAddedTaxRegister = null;
            long? latestFinacialStatement = null;
            long? numberOfEmployees = null;
            bool? isBeingDissolved = null;
            bool? isUnderBankruptcy = null;
            bool? isBeingForciblyDissolved = null;
            bool? isRegisteredVoluntaryOrg = null;
            string homePage = string.Empty;
            string email = string.Empty;
            string sectorCode = string.Empty;
            string sectorCodeDescription = string.Empty;
            string activity = string.Empty;


            long organizationNumber = result["organisasjonsnummer"];
            string organizationName = result["navn"];
            string organizationForm = result["organisasjonsform"]["kode"];

            if (result["institusjonellSektorkode"]["kode"] != null)
            {
                sectorCode = result["institusjonellSektorkode"]["kode"];
                sectorCodeDescription = result["institusjonellSektorkode"]["beskrivelse"];
            }

            if (result["aktivitet"] != null)
            {
                activity = string.Join(',', result["aktivitet"]);
            }

            if (result["hjemmeside"] != null)
            {
                homePage = result["hjemmeside"];
            }

            if (result["epostadresse"] != null)
            {
                email = result["epostadresse"];
            }

            if (result["naeringskode1"] != null)
            {
                industryCode1 = result["naeringskode1"]["kode"];
                industryCode1Description = result["naeringskode1"]["beskrivelse"];
            }

            if (result["registrertIFrivillighetsregisteret"] != null)
            {
                isRegisteredVoluntaryOrg = (bool)result["registrertIFrivillighetsregisteret"];
            } else
            {
                isRegisteredVoluntaryOrg = false;
            }

            if (result["registreringsdatoFrivillighetsregisteret"] != null)
            {
                createdInNonProfitRegistry = Convert.ToDateTime(result["registreringsdatoFrivillighetsregisteret"]);
            } 

            if (result["naeringskode2"] != null)
            {
                industryCode2 = result["naeringskode2"]["kode"];
                industryCode2Description = result["naeringskode2"]["beskrivelse"];
            }

            if (result["naeringskode3"] != null)
            {
                industryCode3 = result["naeringskode3"]["kode"];
                industryCode3Description = result["naeringskode3"]["beskrivelse"];
            }

            if (result["forretningsadresse"] != null)
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

            if (result["registreringsdatoEnhetsregisteret"] != null)
            {
                createdInCentralRegisterForLegalEntities =
                    Convert.ToDateTime(result["registreringsdatoEnhetsregisteret"]);
            }

            if (result["stiftelsesdato"] != null)
            {
                established = Convert.ToDateTime(result["stiftelsesdato"]);
            }

            if (result["registrertIForetaksregisteret"] != null)
            {
                isInRegisterOfBusinessEnterprises = (bool)result["registrertIForetaksregisteret"];
            }

            if (result["registrertIMvaregisteret"] != null)
            {
                isInValueAddedTaxRegister = (bool)result["registrertIMvaregisteret"];
            }

            if (result["sisteInnsendteAarsregnskap"] != null)
            {
                latestFinacialStatement = result["sisteInnsendteAarsregnskap"] ?? 0;
            }

            if (result["antallAnsatte"] != null)
            {
                numberOfEmployees = result["antallAnsatte"];
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

            var ecb = new EvidenceBuilder(_metadata, "UnitBasicInformation");
            ecb.AddEvidenceValue("OrganizationNumber", organizationNumber);
            ecb.AddEvidenceValue("OrganizationName", organizationName);
            ecb.AddEvidenceValue("OrganizationForm", organizationForm);
            ecb.AddEvidenceValue("IsInRegistryOfNonProfitOrganizations", isRegisteredVoluntaryOrg);

            if (homePage != string.Empty)
            {
                ecb.AddEvidenceValue("HomePage", homePage);
            }
            if (email != string.Empty)
            {
                ecb.AddEvidenceValue("Email", email);
            }

            if (createdInNonProfitRegistry != null)
            {
                ecb.AddEvidenceValue("CreatedInNonProfitRegistry", createdInNonProfitRegistry.Value);
            }

            if (industryCode1 != string.Empty)
            {
                ecb.AddEvidenceValue("IndustryCode1", industryCode1);
            }

            if (industryCode1Description != string.Empty)
            {
                ecb.AddEvidenceValue("IndustryCode1Description", industryCode1Description);
            }

            if (industryCode2 != string.Empty)
            {
                ecb.AddEvidenceValue("IndustryCode2", industryCode2);
            }

            if (industryCode2Description != string.Empty)
            {
                ecb.AddEvidenceValue("IndustryCode2Description", industryCode2Description);
            }

            if (industryCode3 != string.Empty)
            {
                ecb.AddEvidenceValue("IndustryCode3", industryCode3);
            }

            if (industryCode3Description != string.Empty)
            {
                ecb.AddEvidenceValue("IndustryCode3Description", industryCode3Description);
            }

            if (businessAddressStreet != string.Empty)
            {
                ecb.AddEvidenceValue("BusinessAddressStreet", businessAddressStreet);
            }

            if (businessAddressZip != string.Empty)
            {
                ecb.AddEvidenceValue("BusinessAddressZip", businessAddressZip);
            }

            if (businessAddressCity != string.Empty)
            {
                ecb.AddEvidenceValue("BusinessAddressCity", businessAddressCity);
            }

            if (postalAddressStreet != string.Empty)
            {
                ecb.AddEvidenceValue("PostalAddressStreet", postalAddressStreet);
            }

            if (postalAddressZip != string.Empty)
            {
                ecb.AddEvidenceValue("PostalAddressZip", postalAddressZip);
            }

            if (postalAddressCity != string.Empty)
            {
                ecb.AddEvidenceValue("PostalAddressCity", postalAddressCity);
            }

            if (postalAddressCountryCode != string.Empty)
            {
                ecb.AddEvidenceValue("PostalAddressCountryCode", postalAddressCountryCode);
            }

            if (createdInCentralRegisterForLegalEntities != null)
            {
                ecb.AddEvidenceValue("CreatedInCentralRegisterForLegalEntities", createdInCentralRegisterForLegalEntities.Value);
            }

            if (established != null)
            {
                ecb.AddEvidenceValue("Established", established.Value);
            }

            if (isInRegisterOfBusinessEnterprises != null)
            {
                ecb.AddEvidenceValue("IsInRegisterOfBusinessEnterprises", isInRegisterOfBusinessEnterprises.Value);
            }

            if (isInValueAddedTaxRegister != null)
            {
                ecb.AddEvidenceValue("IsInValueAddedTaxRegister", isInValueAddedTaxRegister.Value);
            }

            if (latestFinacialStatement != null)
            {
                ecb.AddEvidenceValue("LatestFinacialStatement", latestFinacialStatement.Value);
            }

            if (numberOfEmployees != null)
            {
                ecb.AddEvidenceValue("NumberOfEmployees", numberOfEmployees.Value);
            }

            if (isBeingDissolved != null)
            {
                ecb.AddEvidenceValue("IsBeingDissolved", isBeingDissolved.Value);
            }

            if (isUnderBankruptcy != null)
            {
                ecb.AddEvidenceValue("IsUnderBankruptcy", isUnderBankruptcy.Value);
            }

            if (isBeingForciblyDissolved != null)
            {
                ecb.AddEvidenceValue("IsBeingForciblyDissolved", isBeingForciblyDissolved.Value);
            }

            if (sectorCode != string.Empty)
            {
                ecb.AddEvidenceValue("SectorCode", sectorCode);
                ecb.AddEvidenceValue("SectorCodeDescription", sectorCodeDescription);
            }

            if (activity != string.Empty)
            {
                ecb.AddEvidenceValue("Activity", activity);
            }



            return ecb.GetEvidenceValues();
        }

        private static async Task<dynamic> HarvestFromBrreg(string organization, HttpClient client)
        {
            string rawResult;
            try
            {
                var response = await client.GetAsync(Requests.GetCcrUrlForMainUnit(organization));
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    response = await client.GetAsync(Requests.GetCcrUrlForSubUnit(organization));
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
