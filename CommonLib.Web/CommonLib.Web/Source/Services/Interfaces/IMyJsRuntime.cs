using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CommonLib.Web.Source.Services.Interfaces
{
    public interface IMyJsRuntime
    {
        bool IsInitialized { get; }

        //ValueTask JsVoidFromModule(string modulePath, string functionName);
        //ValueTask JsVoidFromModule(string modulePath, string functionName, params object[] parameters);
        //ValueTask JsVoidFromComponent(string componentName, string functionName);
        //ValueTask JsVoidFromComponent(string componentName, string functionName, params object[] parameters);
        Task<IJSObjectReference> ImportModuleAsync(string modulePath);
        Task<IJSObjectReference> ImportComponentOrPageModuleAsync(string componentOrPageName, NavigationManager nav, HttpClient http);
        Task<IJSObjectReference> ImportComponentOrPageModuleAsync(string componentOrPageName);
        Task<Guid> GetOrCreateSessionIdAsync();
        Task<Guid> GetSessionIdAsync();
        Task<Guid> GetSessionIdOrEmptyAsync();
    }
}
