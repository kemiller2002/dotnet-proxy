using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.IO;


namespace proxy.Controllers
{

    [ApiController]
    [Route("/api")]
    public class Reporting : ControllerBase
    {

        static IEnumerable<string> NonTransferHeaders = new[] { "Origin", "Referrer", "Host", "Accept-Encoding" };

        const string BaseUrl = "metabase-int.reninc.com";

        static void TransferHeaders(string baseUrl, string path, WebRequest request, IHeaderDictionary headers)
        {
            request.Headers.Clear();
            headers
                .Where(x => !NonTransferHeaders.Contains(x.Key))
                .ToList()
                .ForEach(x=>request.Headers.Add(x.Key, x.Value));

            request.Headers.Add("Host",baseUrl);
            request.Headers.Add("Origin",$"https://{baseUrl}");
            request.Headers.Add("Referrer",$"https://{baseUrl}//{path}");
        }

        static void SetResponseHeaders(IHeaderDictionary responseHeaders, WebResponse webResponse)
        {
            responseHeaders.Add("set-cookie", webResponse.Headers["set-cookie"]);
        }


        static async Task HandleRequest (
            string proxyUrl,
            IHeaderDictionary requestHeaders,
            IHeaderDictionary responseHeaders,
            Stream responseStream,
            string method,
            string path)
        {
            var request = WebRequest.Create($"https://{proxyUrl}/{path}");
            request.Method = method;

            TransferHeaders(proxyUrl, path, request, requestHeaders);

            var response = request.GetResponse();
            
            SetResponseHeaders(responseHeaders, response);

            await response.
                GetResponseStream().
                CopyToAsync(responseStream);
        }



        [HttpGet("{*path}")]
        public async Task Get(string path) => 
            await HandleRequest(BaseUrl, Request.Headers, Response.Headers, Response.Body, "GET", path);


        [HttpPost("{*path}")]
        public async Task Post(string path) =>
            await HandleRequest(BaseUrl, Request.Headers, Response.Headers, Response.Body, "POST", path);
    }
}
