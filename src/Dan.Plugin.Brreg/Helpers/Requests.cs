using Microsoft.Extensions.Logging;
using Dan.Common.Exceptions;
using Nadobe.EvidenceSources.ES_BR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Dan.Plugin.Brreg.Models;

namespace Dan.Plugin.Brreg.Helpers
{
    public static class Requests
    {

        public static async Task<dynamic> MakeRequest(string url, HttpClient client, string username, string password, HttpMethod method, ILogger logger)
        {
            string rawResult;
            dynamic result;
            
            try
            {
                var requestMessage = new HttpRequestMessage(method, url);
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    var authenticationString = $"{username}:{password}";
                    var base64EncodedAuthenticationString = Convert.ToBase64String(ASCIIEncoding.UTF8.GetBytes(authenticationString));
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
                }

                var response = await client.SendAsync(requestMessage);

                if (response.IsSuccessStatusCode)
                {
                    rawResult = await response.Content.ReadAsStringAsync();
                    result = JsonConvert.DeserializeObject(rawResult);

                    if (result == null)
                    {
                        logger.LogError($"Failed request for {url} - result is null");
                        throw new EvidenceSourcePermanentServerException(Constants.ERROR_CCR_UPSTREAM_ERROR, "Did not understand the data model returned from upstream source");
                    }
                } else
                {
                    logger.LogError($"Failed request for {url} - resultstatuscode: {response.StatusCode}");
                    throw new EvidenceSourcePermanentServerException(Constants.ERROR_CCR_UPSTREAM_ERROR, "Call to data source failed");
                }

            }
            catch (HttpRequestException e)
            {
                logger.LogError($"Failed request for {url} - Error:{e.Message}");
                throw new EvidenceSourcePermanentServerException(Constants.ERROR_CCR_UPSTREAM_ERROR, null, e);
            }



            return result;
        }

        public static string GetCcrUrlForMainUnit(string organizationNumber)
        {
            var validationUrl = IsSyntheticOrganizationNumber(organizationNumber)
                ? "https://data.ppe.brreg.no/enhetsregisteret/api/enheter/{0}"
                : "https://data.brreg.no/enhetsregisteret/api/enheter/{0}";

            return string.Format(validationUrl, organizationNumber);
        }

        public static string GetCcrUrlForSubUnit(string organizationNumber)
        {
            var validationUrl = IsSyntheticOrganizationNumber(organizationNumber)
                ? "https://data.ppe.brreg.no/enhetsregisteret/api/underenheter/{0}"
                : "https://data.brreg.no/enhetsregisteret/api/underenheter/{0}";

            return string.Format(validationUrl, organizationNumber);
        }


        private static bool IsSyntheticOrganizationNumber(string organizationNumber)
        {
            if (organizationNumber == null) return false;
            return organizationNumber.StartsWith("2") || organizationNumber.StartsWith("3");
        }


        public static async Task<T> GetData<T>(HttpClient client, string url, ILogger logger) where T : new()
        {
            var response = await client.GetAsync(url);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError(responseContent);

                if (response.StatusCode == HttpStatusCode.InternalServerError)
                {
                    throw new EvidenceSourceTransientException(Constants.ERROR_CCR_UPSTREAM_ERROR, "Remote endpoint does not respond. Try again later.");
                } else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new EvidenceSourceTransientException(Constants.ERROR_AUTHENTICATION, "Remote authentication failed.");
                } else if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new EvidenceSourcePermanentClientException(Constants.ERROR_PERSON_NOT_FOUND, "Invalid identification number");
                } else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new EvidenceSourcePermanentClientException(Constants.ERROR_PERSON_NOT_FOUND, "Invalid identification number, subject not found");
                } else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    throw new EvidenceSourceTransientException(Constants.ERROR_CCR_UPSTREAM_ERROR, "Could not access Remote endpoint. Try again later.");
                }
                else 
                {
                    throw new EvidenceSourceTransientException(Constants.ERROR_UNKNOWN, "Unknown status code");
                }
            }
            else
            {
                return JsonConvert.DeserializeObject<T>(responseContent);
            }
        }


    }
}
