using System.Collections.Generic;
using System.Net.Http;
using AwesomeAssertions;
using Dan.Common.Interfaces;
using Dan.Plugin.Brreg.Config;
using Dan.Plugin.Brreg.Models.EktepaktV2;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Ektepakt = Nadobe.EvidenceSources.ES_BR.Ektepakt;

namespace Dan.Plugin.Brreg.Test
{
    public class EktepaktTest
    {
        private Ektepakt _ektepakt;

        public EktepaktTest()
        {
            var client = A.Fake<IHttpClientFactory>();
            IOptions<Settings> settingsOptions = A.Fake<IOptions<Settings>>();
            ILoggerFactory loggerFactoryMock = A.Fake<ILoggerFactory>();
            IEvidenceSourceMetadata metadata = A.Fake<IEvidenceSourceMetadata>();

            _ektepakt = new(client, settingsOptions, metadata, loggerFactoryMock);
        }

        [Theory]
        [InlineData("fornavn", "mellomnavn", "etternavn", "fornavn mellomnavn etternavn")]
        [InlineData("fornavn", null, "etternavn", "fornavn etternavn")]
        public void MappingEktepaktContainsMiddlename(string fornavn, string? mellomnavn, string etternavn, string expected)
        {
            // Arrange
            var ektepakter = new List<Models.EktepaktV2.Ektepakt>
            {
                new Models.EktepaktV2.Ektepakt{
                    rolle =
                    [
                        new Rolle(),
                        new Rolle
                        {
                            person = new Person()
                            {
                                navn = new Navn
                                {
                                    etternavn = etternavn,
                                    fornavn = fornavn,
                                    mellomnavn = mellomnavn
                                },
                                adresse = new Adresse()
                            },
                            avtaleinnhold = null
                        }
                    ]
                }
            };
            var input = new EktepaktV2
            {
                ektepakt = ektepakter.ToArray()
            };

            //Act
            var mapped = _ektepakt.MapEktepaktDD(input);

            //Assert
            mapped.Ektepakter[0].SpouseName.Should().Be(expected);
        }
    }
}
