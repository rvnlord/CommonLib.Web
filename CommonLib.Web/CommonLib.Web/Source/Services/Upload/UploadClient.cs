using System;
using System.Net.Http;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Blazored.SessionStorage;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Source.Models;
using CommonLib.Source.Models.Interfaces;
using CommonLib.Web.Source.Controllers;
using CommonLib.Web.Source.Services.Account.Interfaces;
using CommonLib.Web.Source.Services.Interfaces;
using CommonLib.Web.Source.Services.Upload.Interfaces;

namespace CommonLib.Web.Source.Services.Upload
{
    public class UploadClient : IUploadClient
    {
        private HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        private readonly ISessionStorageService _sessionStorage;
        private readonly IMyJsRuntime _myJsRuntime;
        private readonly IAccountClient _accountClient;

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

        public UploadClient(HttpClient httpClient, IAccountClient accountClient, ILocalStorageService localStorage, ISessionStorageService sessionStorage, IMyJsRuntime myJsRuntime)
        {
            HttpClient = httpClient;
            _accountClient = accountClient;
            _localStorage = localStorage;
            _sessionStorage = sessionStorage;
            _myJsRuntime = myJsRuntime;
        }

        public async Task<IApiResponse> UploadChunkToUserFolderAsync(FileData chunk)
        {
            var authUser = (await _accountClient.GetAuthenticatedUserAsync())?.Result;
            return await HttpClient.PostJTokenAsync<ApiResponse>($"api/upload/{nameof(UploadApiController.UploadChunkToUserFolderAsync)}", new
            {
                AuthenticatedUser = authUser, 
                Chunk = chunk
            });
        }
    }
}
