using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dan.Common.Exceptions;
using Dan.Plugin.Brreg.Models;
using Microsoft.Extensions.Logging;

namespace Dan.Plugin.Brreg.Helpers
{
    /// <summary>
    /// Fetches the firmaattest PDF from virksomhet.brreg.no via the site's Next.js server action "generatePdf"
    /// (the one behind the "Firmaattest" button). Unofficial; may break when the site changes.
    /// </summary>
    internal static class Firmaattest
    {
        private const string Base = "https://virksomhet.brreg.no";

        // RSC stream: binary rows look like "<id>:o<hexlen>,<bytes>".
        private static readonly Regex BinaryRow = new(@"\n\w+:o([0-9a-f]+),", RegexOptions.Compiled);

        private static readonly Regex ChunkUrl = new(@"/_next/static/chunks/[^""]+\.js", RegexOptions.Compiled);

        private static readonly Regex GeneratePdfAction =
            new(@"createServerReference\)\(""([0-9a-f]+)"",[^)]*""generatePdf""\)", RegexOptions.Compiled);

        /// <summary>
        /// The public lookup page for the organization, which is where the firmaattest PDF is generated from.
        /// </summary>
        public static string GetLookupPageUrl(string orgnr, bool underenhet = false)
        {
            return $"{Base}/nb/oppslag/{(underenhet ? "underenheter" : "enheter")}/{orgnr}";
        }

        public static async Task<byte[]> DownloadAsync(HttpClient client, string orgnr, ILogger logger, bool underenhet = false)
        {
            var pageUrl = GetLookupPageUrl(orgnr, underenhet);
            var actionId = await FindActionIdAsync(client, pageUrl, logger);

            var body = $$"""[{"withSignature":true,"enhetsType":"{{(underenhet ? "underenhet" : "hovedenhet")}}","orgnr":"{{orgnr}}","openSections":"firmaattest","lang":"nb"}]""";
            using var request = new HttpRequestMessage(HttpMethod.Post, pageUrl)
            {
                Content = new StringContent(body, Encoding.UTF8, "text/plain"),
            };
            request.Headers.Add("Next-Action", actionId);
            request.Headers.Add("Accept", "text/x-component");

            using var response = await Requests.Send(client, request, logger);
            Requests.EnsureSuccess(response, pageUrl, logger);
            var bytes = await response.Content.ReadAsByteArrayAsync();

            // Latin1 maps each byte to one char, so string offsets equal byte offsets.
            var text = Encoding.Latin1.GetString(bytes);
            var match = BinaryRow.Match(text);
            if (!match.Success)
            {
                logger.LogError($"No firmaattest PDF in response for {orgnr}: {text[..Math.Min(300, text.Length)]}");
                throw new EvidenceSourcePermanentClientException(Constants.ERROR_CERTIFICATE_OF_REGISTRATION_NOT_AVAILABLE,
                    "Unable to retrieve certificate of registration for this organization");
            }

            var length = int.Parse(match.Groups[1].Value, NumberStyles.HexNumber);
            return bytes.AsSpan(match.Index + match.Length, length).ToArray();
        }

        // The action ID changes on every deploy, so look it up from the page's JS chunks.
        private static async Task<string> FindActionIdAsync(HttpClient client, string pageUrl, ILogger logger)
        {
            using var pageResponse = await Requests.Send(client, HttpMethod.Get, pageUrl, logger);
            if (pageResponse.StatusCode == HttpStatusCode.NotFound)
            {
                throw new EvidenceSourcePermanentClientException(Constants.ERROR_CERTIFICATE_OF_REGISTRATION_NOT_AVAILABLE,
                    "Organization not found");
            }
            Requests.EnsureSuccess(pageResponse, pageUrl, logger);
            var html = await pageResponse.Content.ReadAsStringAsync();

            var chunks = ChunkUrl.Matches(html).Select(x => x.Value).Distinct();
            foreach (var chunk in chunks)
            {
                var chunkUrl = Base + chunk;
                using var chunkResponse = await Requests.Send(client, HttpMethod.Get, chunkUrl, logger);
                Requests.EnsureSuccess(chunkResponse, chunkUrl, logger);
                var js = await chunkResponse.Content.ReadAsStringAsync();

                var match = GeneratePdfAction.Match(js);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            logger.LogError($"generatePdf server action not found on {pageUrl} - the site has probably changed");
            throw new EvidenceSourcePermanentServerException(Constants.ERROR_CCR_UPSTREAM_ERROR,
                "Unable to locate the certificate of registration download at the data source");
        }
    }
}
