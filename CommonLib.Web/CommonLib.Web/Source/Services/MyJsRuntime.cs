using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Blazored.SessionStorage;
using CommonLib.Web.Source.Common.Components;
using CommonLib.Web.Source.Common.Utils;
using CommonLib.Web.Source.Services.Interfaces;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.UtilClasses;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using RestSharp;
using CommonLib.Web.Source.Common.Extensions;

namespace CommonLib.Web.Source.Services
{
    public class MyJsRuntime : IMyJsRuntime
    {
        private readonly IJSRuntime _jsRuntime;
        private HttpClient _httpClient;
        private NavigationManager _navigationManager;
        private readonly ISessionStorageService _sessionStorage;
        private static string _commonWwwRootDir;
        private static string _currentWwwRootDir;
        private static bool? _isProduction;

        public static string CommonWwwRootDir => _commonWwwRootDir ??= FileUtils.GetAspNetWwwRootDir<MyJsRuntime>();
        public static string CurrentWwwRootDir => _currentWwwRootDir ??= ((object) WebUtils.ServerHostEnvironment).GetProperty<string>("WebRootPath");
        public static bool IsProduction => _isProduction ??= Directory.Exists(PathUtils.Combine(PathSeparator.BSlash, CurrentWwwRootDir, "_content"));

        public Task<bool> IsInitializedAsync() => _jsRuntime.IsInitializedAsync();

        public MyJsRuntime(IJSRuntime jsRuntime, HttpClient httpClient, NavigationManager navigationManager, ISessionStorageService sessionStorage)
        {
            _jsRuntime = jsRuntime;
            _httpClient = httpClient;
            _navigationManager = navigationManager;
            _sessionStorage = sessionStorage;
        }

        public async Task<IJSObjectReference> ImportModuleAsync(string modulePath)
        {
            if (modulePath == null)
                return null;

            //var importedModules = (await _sessionStorage.GetItemAsStringAsync("ImportedModules"))?.JsonDeserialize().To<List<string>>();

            //if (!importedModules.ContainsIgnoreCase(modulePath))
            //{
                
            //}

            return await _jsRuntime.ImportModuleAndRetryIfCancelledAsync(modulePath);
        }

        public async Task<IJSObjectReference> ImportComponentOrPageModuleAsync(string componentOrPageName, NavigationManager nav, HttpClient http)
        {
            return await ImportModuleAsync(await GetModulePathAsync(componentOrPageName, nav, http));
        }

        public async Task<IJSObjectReference> ImportComponentOrPageModuleAsync(string componentOrPageName)
        {
            return await ImportModuleAsync(await GetModulePathAsync(componentOrPageName, _navigationManager, _httpClient));
        }

        private static string NormalizeComponentOrPageName(string moduleName)
        {
            moduleName = moduleName.Replace("NavBar", "Navbar").Replace("DropDown", "Dropdown").Replace("FileUpload", "Fileupload");
            if (!moduleName.Contains('-'))
                moduleName = moduleName.Take(1).ToLowerInvariant() + moduleName.Skip(1).Replace("_", "/").PascalCaseToKebabCase(); // to preserve "_layout.js"
            return moduleName;
        }

        private static async Task<string> GetModulePathAsync(string componentOrPageName, NavigationManager nav, HttpClient http)
        {
            var componentOrPageNormalized = NormalizeComponentOrPageName(componentOrPageName);
            const string js = ".js";
            const string pagePrefix = "./js/";
            const string componentPrefix = "./js/_components/";
            var commonProjName = "CommonLib.Web"; //componentProjectDir.AfterLast(@"\");
            var commonComponentPrefix = $"./_content/{commonProjName}/js/_components/";
            var commonPagePrefix = $"./_content/{commonProjName}/js/_pages/";

            var virtualPrefix = nav.BaseUri;
            var virtualCommonComponentPath = PathUtils.Combine(PathSeparator.FSlash, virtualPrefix, commonComponentPrefix, componentOrPageNormalized, js);
            var virtualCommonPagePath = PathUtils.Combine(PathSeparator.FSlash, virtualPrefix, commonPagePrefix, componentOrPageNormalized, js);
            var virtualLocalComponentPath = PathUtils.Combine(PathSeparator.FSlash, virtualPrefix, componentPrefix, componentOrPageNormalized, js);
            var virtualLocalPagePath = PathUtils.Combine(PathSeparator.FSlash, virtualPrefix, pagePrefix, componentOrPageNormalized, js);
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("browser"))) // if WebAssembly
            {
                if (http.GetField<bool>("_disposed"))
                    http = new HttpClient { BaseAddress = http.BaseAddress };

                if ((await http.GetAsync(virtualCommonComponentPath)).IsSuccessStatusCode)
                    return virtualCommonComponentPath;
                if ((await http.GetAsync(virtualCommonPagePath)).IsSuccessStatusCode)
                    return virtualCommonPagePath;
                if ((await http.GetAsync(virtualLocalComponentPath)).IsSuccessStatusCode)
                    return virtualLocalComponentPath;
                if ((await http.GetAsync(virtualLocalPagePath)).IsSuccessStatusCode)
                    return virtualLocalPagePath;
            }
            else
            {
                var physicalCommonComponentPath = !IsProduction 
                    ? PathUtils.Combine(PathSeparator.BSlash, CommonWwwRootDir, componentPrefix, componentOrPageNormalized, js)
                    : PathUtils.Combine(PathSeparator.BSlash, CurrentWwwRootDir, "_content", commonProjName, componentPrefix, componentOrPageNormalized, js);
                var physicalCommonPagePath = !IsProduction 
                    ? PathUtils.Combine(PathSeparator.BSlash, CommonWwwRootDir, pagePrefix, componentOrPageNormalized, js)
                    : PathUtils.Combine(PathSeparator.BSlash, CurrentWwwRootDir, "_content", commonProjName, pagePrefix, componentOrPageNormalized, js);
                var physicalLocalComponentPath = PathUtils.Combine(PathSeparator.BSlash, CurrentWwwRootDir, componentPrefix, componentOrPageNormalized, js);
                var physicalLocalPagePath = PathUtils.Combine(PathSeparator.BSlash, CurrentWwwRootDir, pagePrefix, componentOrPageNormalized, js);

                if (File.Exists(physicalCommonComponentPath))
                    return virtualCommonComponentPath;
                if (File.Exists(physicalCommonPagePath))
                    return virtualCommonPagePath;
                if (File.Exists(physicalLocalComponentPath))
                    return virtualLocalComponentPath;
                if (File.Exists(physicalLocalPagePath))
                    return virtualLocalPagePath;
            }
            
            Logger.For<MyJsRuntime>().Warn($"There is no \"{componentOrPageNormalized}\" page or component");
            return null;
        }

        public async Task<Guid> GetOrCreateSessionIdAsync()
        {
            var sessionId = await ParseSessionIdAsync();
            if (sessionId == Guid.Empty)
                sessionId = Guid.NewGuid();

            Logger.For<MyJsRuntime>().Info($"STARTING: _jsRuntime.ForceInvokeVoidAsync(\"sessionStorage.setItem\", \"SessionId\", {sessionId})");

            await _jsRuntime.ForceInvokeVoidAsync("sessionStorage.setItem", "SessionId", sessionId.ToString()).AsTask();

            Logger.For<MyJsRuntime>().Info($"DONE: _jsRuntime.ForceInvokeVoidAsync(\"sessionStorage.setItem\", \"SessionId\", {sessionId})");
            return sessionId;
        }

        private async Task<Guid> ParseSessionIdAsync()
        {
            var isInitialized = await _jsRuntime.IsInitializedAsync();
            if (!isInitialized)
                return Guid.Empty;

            Logger.For<MyJsRuntime>().Info("STARTING: _jsRuntime.ForceInvokeAsync<string>(\"sessionStorage.getItem\", \"SessionId\")");

            var strSessId = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", "SessionId").AsTask();

            Logger.For<MyJsRuntime>().Info($"DONE: _jsRuntime.ForceInvokeAsync<string>(\"sessionStorage.getItem\", \"SessionId\") = {strSessId}");
            var isSessionIdParsable = Guid.TryParse(strSessId, out var sessionId);
            return isSessionIdParsable ? sessionId : Guid.Empty;
        }

        public async Task<Guid> GetSessionIdAsync()
        {
            var sessionId = await ParseSessionIdAsync();
            return sessionId == Guid.Empty ? throw new NullReferenceException("\"SessionId\" not present") : sessionId;
        }

        public async Task<Guid> GetSessionIdOrEmptyAsync()
        {
            return await ParseSessionIdAsync();
        }
    }
}

