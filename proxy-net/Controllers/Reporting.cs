using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;

namespace proxy.Controllers
{

    //[Route("/reporting")]
    public class Reporting : ApiController
    {

        static IEnumerable<string> NonTransferHeaders = new[] { "Origin", "Referrer", "Host", "Accept-Encoding" };

        const string BaseUrl = "metabase-int.reninc.com";

        static void TransferHeaders(string baseUrl, string path, WebRequest request, HttpRequestHeaders headers)
        {
            request.Headers.Clear();
            headers
                .Where(x => !NonTransferHeaders.Contains(x.Key))
                .ToList()
                .ForEach(x=>request.Headers.Add(x.Key, x.Value.First()));

            request.Headers.Add("Host",baseUrl);
            request.Headers.Add("Origin",$"https://{baseUrl}");
            request.Headers.Add("Referrer",$"https://{baseUrl}//{path}");
        }

        static void SetResponseHeaders(HttpContentHeaders responseHeaders, WebResponse webResponse)
        {
            responseHeaders.Add("set-cookie", webResponse.Headers["set-cookie"]);
        }

        static async Task<HttpResponseMessage> HandleRequest (
            string proxyUrl,
            HttpRequestHeaders requestHeaders,
            string method,
            string path)
        {
            var request = WebRequest.Create($"https://{proxyUrl}/{path}");
            request.Method = method;

            TransferHeaders(proxyUrl, path, request, requestHeaders);

            var response = request.GetResponse();

            var result = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(response.GetResponseStream()),
            };

            SetResponseHeaders(result.Content.Headers, response);

            return result;
        }

        [HttpGet][Route("{*path}")]
        public async Task Get(string path) =>await HandleRequest(BaseUrl, this.Request.Headers, "GET", path);


        [HttpPost][Route("{*path}")]
        public async Task Post(string path) =>
            await HandleRequest(BaseUrl, Request.Headers, "POST", path);
    }
}
