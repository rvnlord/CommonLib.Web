using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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
        private static string _commonWwwRootDir;
        private static string _currentWwwRootDir;

        public static string CommonWwwRootDir => _commonWwwRootDir ??= FileUtils.GetAspNetWwwRootDir<MyJsRuntime>();
        public static string CurrentWwwRootDir => _currentWwwRootDir ??= FileUtils.GetCurrentProjectAspNetWwwRootDir();

        public MyJsRuntime(IJSRuntime jsRuntime, HttpClient httpClient, NavigationManager navigationManager)
        {
            _jsRuntime = jsRuntime;
            _httpClient = httpClient;
            _navigationManager = navigationManager;
        }

        //public async ValueTask JsVoidFromModule(string modulePath, string functionName)
        //{
        //    var jsModule = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", modulePath).ConfigureAwait(false);
        //    await jsModule.InvokeVoidAsync(functionName).ConfigureAwait(false);
        //    await jsModule.DisposeAsync();
        //}

        //public async ValueTask JsVoidFromModule(string modulePath, string functionName, params object[] paramsters)
        //{
        //    var jsModule = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", modulePath).ConfigureAwait(false);
        //    await jsModule.InvokeVoidAsync(functionName, paramsters).ConfigureAwait(false);
        //    await jsModule.DisposeAsync();
        //}

        //public async ValueTask JsVoidFromComponent(string componentName, string functionName)
        //{
        //    await JsVoidFromModule(GetModulePath(componentName), functionName).ConfigureAwait(false);
        //} // `.` before the path is mandatory, otherwise it won't work

        //public async ValueTask JsVoidFromComponent(string componentName, string functionName, params object[] paramsters)
        //{
        //    await JsVoidFromModule(GetModulePath(componentName), functionName, paramsters).ConfigureAwait(false);
        //}

        public async Task<IJSObjectReference> ImportModuleAsync(string modulePath)
        {
            if (modulePath == null)
                return null;
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
            moduleName = moduleName.Replace("NavBar", "Navbar").Replace("DropDown", "Dropdown");
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
                var physicalCommonComponentPath = PathUtils.Combine(PathSeparator.BSlash, CommonWwwRootDir, componentPrefix, componentOrPageNormalized, js);
                var physicalCommonPagePath = PathUtils.Combine(PathSeparator.BSlash, CommonWwwRootDir, pagePrefix, componentOrPageNormalized, js);
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
    }
}

