using System;
using System.Threading.Tasks;
using CommonLib.Web.Source.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CommonLib.Web.Source.Services.Interfaces
{
    public interface IJQuery
    {
        IJSRuntime JsRuntime { get; }
        Task<JQueryCollection> QueryAsync(string selectors);
        Task<JQueryCollection> QueryAsync(Guid[] guids);
        Task<JQueryCollection> QueryAsync(ElementReference selector);
        Task<Models.JQuery> QueryOneAsync(string selector);
        Task<Models.JQuery> QueryOneAsync(ElementReference selector);
        Task<Models.JQuery> QueryOneAsync(Guid id);
       
        public static event Func<Task> OnWindowResizingAsync;

        [JSInvokable]
        public static async Task OnWindowResizedAsync()
        {
            if (OnWindowResizingAsync != null) 
                await OnWindowResizingAsync.Invoke().ConfigureAwait(false);
        }
    }
}
