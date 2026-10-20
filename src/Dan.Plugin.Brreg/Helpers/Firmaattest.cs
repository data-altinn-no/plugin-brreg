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

        // The action id only changes when the site is deployed, so it is cached across requests and rediscovered
        // when a cached id stops producing a PDF.
        private static readonly TimeSpan ActionIdTtl = TimeSpan.FromHours(1);
        private static readonly object ActionIdLock = new();
        private static string _actionId;
        private static DateTime _actionIdExpires;

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

            var cachedActionId = GetCachedActionId();
            var actionId = cachedActionId ?? await FindActionIdAsync(client, pageUrl, logger);

            // With a cached id, an unknown-action response may just mean the site was redeployed: rediscover once and retry.
            // Other failures are thrown as classified errors without touching the cache.
            var pdf = await TryGeneratePdfAsync(client, pageUrl, actionId, orgnr, underenhet, logger, throwOnFailure: cachedActionId == null);
            if (pdf == null)
            {
                logger.LogInformation($"Cached generatePdf action id did not produce a PDF for {orgnr}, rediscovering");
                InvalidateActionId();
                actionId = await FindActionIdAsync(client, pageUrl, logger);
                pdf = await TryGeneratePdfAsync(client, pageUrl, actionId, orgnr, underenhet, logger, throwOnFailure: true);
            }

            return pdf;
        }

        /// <summary>
        /// Calls the generatePdf server action and extracts the PDF from the RSC response. When <paramref name="throwOnFailure"/>
        /// is false, the responses that indicate an unknown action id (404, or a successful response without a binary row)
        /// return null so the caller can retry with a fresh id. Every other failure, such as 401, 403 or 5xx, is thrown as a
        /// classified DAN exception regardless, since rediscovering the action id cannot help there.
        /// </summary>
        private static async Task<byte[]> TryGeneratePdfAsync(HttpClient client, string pageUrl, string actionId, string orgnr,
            bool underenhet, ILogger logger, bool throwOnFailure)
        {
            var body = $$"""[{"withSignature":true,"enhetsType":"{{(underenhet ? "underenhet" : "hovedenhet")}}","orgnr":"{{orgnr}}","openSections":"firmaattest","lang":"nb"}]""";
            using var request = new HttpRequestMessage(HttpMethod.Post, pageUrl)
            {
                Content = new StringContent(body, Encoding.UTF8, "text/plain"),
            };
            request.Headers.Add("Next-Action", actionId);
            request.Headers.Add("Accept", "text/x-component");

            using var response = await Requests.Send(client, request, logger);
            if (response.StatusCode == HttpStatusCode.NotFound && !throwOnFailure)
            {
                return null;
            }
            Requests.EnsureSuccess(response, pageUrl, logger);

            var bytes = await response.Content.ReadAsByteArrayAsync();

            // Latin1 maps each byte to one char, so string offsets equal byte offsets.
            var text = Encoding.Latin1.GetString(bytes);
            var match = BinaryRow.Match(text);
            if (!match.Success)
            {
                if (!throwOnFailure)
                {
                    return null;
                }
                logger.LogError($"No firmaattest PDF in response for {orgnr}: {text[..Math.Min(300, text.Length)]}");
                throw new EvidenceSourcePermanentClientException(Constants.ERROR_CERTIFICATE_OF_REGISTRATION_NOT_AVAILABLE,
                    "Unable to retrieve certificate of registration for this organization");
            }

            var length = int.Parse(match.Groups[1].Value, NumberStyles.HexNumber);
            var start = match.Index + match.Length;
            if (length <= 0 || start + length > bytes.Length)
            {
                logger.LogError($"Firmaattest binary row for {orgnr} declares {length} bytes but only {bytes.Length - start} are available");
                throw new EvidenceSourceTransientException(Constants.ERROR_CCR_UPSTREAM_ERROR,
                    "Incomplete certificate of registration received from the data source");
            }

            var pdf = bytes.AsSpan(start, length);
            if (!pdf.StartsWith("%PDF"u8))
            {
                logger.LogError($"Firmaattest binary row for {orgnr} is not a PDF");
                throw new EvidenceSourcePermanentServerException(Constants.ERROR_CCR_UPSTREAM_ERROR,
                    "Unexpected certificate of registration format at the data source");
            }

            return pdf.ToArray();
        }

        private static string GetCachedActionId()
        {
            lock (ActionIdLock)
            {
                return DateTime.UtcNow < _actionIdExpires ? _actionId : null;
            }
        }

        private static void CacheActionId(string actionId)
        {
            lock (ActionIdLock)
            {
                _actionId = actionId;
                _actionIdExpires = DateTime.UtcNow + ActionIdTtl;
            }
        }

        private static void InvalidateActionId()
        {
            lock (ActionIdLock)
            {
                _actionId = null;
                _actionIdExpires = DateTime.MinValue;
            }
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
                if (!chunkResponse.IsSuccessStatusCode)
                {
                    // A stale chunk reference after a deploy should not stop the scan of the remaining chunks.
                    logger.LogWarning($"Skipping chunk {chunkUrl} - status {(int)chunkResponse.StatusCode}");
                    continue;
                }
                var js = await chunkResponse.Content.ReadAsStringAsync();

                var match = GeneratePdfAction.Match(js);
                if (match.Success)
                {
                    CacheActionId(match.Groups[1].Value);
                    return match.Groups[1].Value;
                }
            }

            logger.LogError($"generatePdf server action not found on {pageUrl} - the site has probably changed");
            throw new EvidenceSourcePermanentServerException(Constants.ERROR_CCR_UPSTREAM_ERROR,
                "Unable to locate the certificate of registration download at the data source");
        }
    }
}
