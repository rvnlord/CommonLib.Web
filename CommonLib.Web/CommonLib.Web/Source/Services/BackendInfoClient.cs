using System;
using System.Net.Http;
using System.Threading.Tasks;
using CommonLib.Web.Source.Models;
using CommonLib.Web.Source.Services.Interfaces;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Source.Models;
using CommonLib.Web.Source.Common.Utils;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using NuGet.ProjectModel;

namespace CommonLib.Web.Source.Services
{
    public class BackendInfoClient : IBackendInfoClient
    {
        private readonly IConfiguration _conf;
        private HttpClient _httpClient;

        public HttpClient HttpClient
        {
            get
            {
                if (_httpClient.BaseAddress == null && ConfigUtils.BackendBaseUrl != null)
                    _httpClient.BaseAddress = new Uri(ConfigUtils.BackendBaseUrl);
                return _httpClient;
            }
            set =>  _httpClient = value;
        }

        public BackendInfoClient(HttpClient httpClient, IConfiguration conf)
        {
            HttpClient = httpClient;
            _conf = conf;
            //ConfigUtils.BackendBaseUrlChanged += (_, e) =>
            //{
            //    HttpClient.BaseAddress ??= new Uri(e.ChangedTo);
            //};
        }
        
        public async Task<ApiResponse<ExtendedTime>> PingAsync()
        {
            var pingResp = await HttpClient.GetJTokenAsync<ApiResponse<JToken>>("api/backendinfo/ping");
            return pingResp.IsError 
                ? new ApiResponse<ExtendedTime>(pingResp.StatusCode, pingResp.Message, null, null, pingResp.ResponseException) 
                : new ApiResponse<ExtendedTime>(StatusCodeType.Status200OK, "Server Pinged Successfully", null, pingResp.Result["TimeStamp"].ToExtendedTime());
        }

        public async Task<ApiResponse> SetFrontendBaseUrlAsync(string frontendBaseUrl)
        {
            return await HttpClient.PostJTokenAsync<ApiResponse>("api/backendinfo/setfrontendbaseurl", frontendBaseUrl);
        }

        public async Task<ApiResponse> SetBackendBaseUrlAsync(string backendBaseUrl)
        {
            return await HttpClient.PostJTokenAsync<ApiResponse>("api/backendinfo/setbackendbaseurl", backendBaseUrl);
        }

        public async Task<ApiResponse<string>> GetBackendDBCSAsync()
        {
            var dbcsResp = await HttpClient.GetJTokenAsync<ApiResponse<string>>("api/backendinfo/getbackenddbcs");
            if (dbcsResp.IsError)
                return dbcsResp;

            var key = _conf.GetSection("CryptographyKeys").GetValue<string>("DBCS");
            dbcsResp.Result = dbcsResp.Result.Base58ToByteArray().DecryptCamellia(key.Base58ToByteArray()).ToUTF8String();
            return dbcsResp;
        }
    }
}
