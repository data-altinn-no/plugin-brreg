using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dan.Common;
using Dan.Common.Interfaces;
using Dan.Common.Models;
using Dan.Plugin.Brreg;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using global::ES_BR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace Nadobe.EvidenceSources.ES_BR {


    /// <summary>
    /// The class implementing the mandatory metadata functions
    /// </summary>
    public class Metadata : IEvidenceSourceMetadata
    {

        /// <summary>
        /// Mandatory "evidencecodes" function. Must return a list of evidence codes this repository implements.
        /// </summary>
        /// <param name="req">The HTTP request</param>
        /// <param name="log">The logging object</param>
        /// <returns>A HTTP response</returns>
        [Function(Constants.EvidenceSourceMetadataFunctionName)]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req, FunctionContext context)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(GetEvidenceCodes());
            return response;
        }

        /// <inheritdoc />
        public List<EvidenceCode> GetEvidenceCodes()
        {
            return new List<EvidenceCode>
            {
                CertificateOfRegistration.GetDefinition(),
                CertificateOfRegistration.GetDefinitionOpen(),
                AnnualFinancialReport.GetDefinition(),
                AnnualFinancialReport.GetDefinitionOpen(),
                UnitBasicInformation.GetDefinition(),              
                Konkurs.GetDefinition(),               
                Regnskapsregisteret.GetDefinitionRegnskap(),
                Regnskapsregisteret.GetDefinitionRegnskapId(),
                Regnskapsregisteret.GetDefinitionRegnskapOpen(),
                Roller.GetDefinition(),
                Kunngjoringer.GetDefinition(),
                Ektepakt.GetDefinitionEktepaktV2(),
                Losore.GetDefinitionRettsstiftelserKjoretoy(),
                //Losore.GetDefinitionRettsstiftelserPerson(),
                Losore.GetDefinitionRettsstiftelserVirksomhet(),
                Stotteregisteret.GetDefinition(),
                Tilskuddsregisteret.GetDefinition(),
                Registerutskrift.GetDefinition(),
                Losore.GetDefinitionRettsstiftelserVirksomhetOpen(),
                Frivillighetsregisteret.GetDefinitionFrivilligOrganisation(),
                UnitBasicInformation.GetDefinitionVirksomhetsinformasjon(),
            };
        }
    }
}
