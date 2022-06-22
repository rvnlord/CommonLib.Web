using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CommonLib.Web.Source.Services.Interfaces
{
    public interface IMyJsRuntime
    {
        //ValueTask JsVoidFromModule(string modulePath, string functionName);
        //ValueTask JsVoidFromModule(string modulePath, string functionName, params object[] parameters);
        //ValueTask JsVoidFromComponent(string componentName, string functionName);
        //ValueTask JsVoidFromComponent(string componentName, string functionName, params object[] parameters);
        public Task<IJSObjectReference> ImportModuleAsync(string modulePath);
        public Task<IJSObjectReference> ImportComponentOrPageModuleAsync(string componentOrPageName, NavigationManager nav, HttpClient http);
        public Task<IJSObjectReference> ImportComponentOrPageModuleAsync(string componentOrPageName);
    }
}
