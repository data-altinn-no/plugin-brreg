using System.Collections.Generic;
using AwesomeAssertions;
using System.Net.Http;
using Dan.Common.Interfaces;
using Dan.Plugin.Brreg.Config;
using Dan.Plugin.Brreg.Models.EktepaktV2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Dan.Plugin.Brreg.Test
{
    public class EktepaktTest
    {
        private Nadobe.EvidenceSources.ES_BR.Ektepakt _ektepakt;

        public EktepaktTest()
        {
            var client = new Mock<IHttpClientFactory>();
            Mock<IOptions<Settings>> settingsOptions = new();
            Mock<ILoggerFactory> loggerFactoryMock = new();
            Mock<IEvidenceSourceMetadata> metadata = new();

            _ektepakt = new(client.Object, settingsOptions.Object, metadata.Object, loggerFactoryMock.Object);
        }

        [Theory]
        [InlineData("fornavn", "mellomnavn", "etternavn", "fornavn mellomnavn etternavn")]
        [InlineData("fornavn", null, "etternavn", "fornavn etternavn")]
        public void MappingEktepaktContainsMiddlename(string fornavn, string? mellomnavn, string etternavn, string expected)
        {
            // Arrange
            var ektepakter = new List<Ektepakt>
            {
                new Ektepakt{
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
