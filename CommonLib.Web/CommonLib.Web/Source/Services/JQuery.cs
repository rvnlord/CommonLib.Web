using System;
using System.Linq;
using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Converters;
using CommonLib.Web.Source.Common.Extensions.Collections;
using CommonLib.Web.Source.Models;
using CommonLib.Web.Source.Services.Interfaces;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions.Collections;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CommonLib.Web.Source.Services
{
    public class JQuery : IJQuery
    {
        public IJSRuntime JsRuntime { get; }

        public JQuery(IJSRuntime jsRuntime)
        {
            JsRuntime = jsRuntime;
        }

        public async Task<JQueryCollection> QueryAsync(string selector)
        {
            return (await JsRuntime.InvokeAsync<string>("BlazorJQueryUtils.Query", selector).ConfigureAwait(false)).JsonDeserialize().ToJQueryCollection(this);
        }

        public async Task<JQueryCollection> QueryAsync(ElementReference selector)
        {
            return (await JsRuntime.InvokeAsync<string>("BlazorJQueryUtils.Query", selector).ConfigureAwait(false)).JsonDeserialize().ToJQueryCollection(this);
        }

        public async Task<Models.JQuery> QueryOneAsync(string selector)
        {
            return await QueryAsync(selector).FirstAsync().ConfigureAwait(false);
        }

        public async Task<Models.JQuery> QueryOneAsync(ElementReference selector)
        {
            return await QueryAsync(selector).FirstAsync().ConfigureAwait(false);
        }

        public async Task<JQueryCollection> QueryAsync(Guid[] guids)
        {
            return await QueryAsync(guids.Select(guid => $"[my-guid='{guid}']").JoinAsString(", ")).ConfigureAwait(false);
        }

        public async Task<Models.JQuery> QueryOneAsync(Guid id)
        {
            return (await QueryAsync(new[] { id }).ConfigureAwait(false)).SingleOrDefault();
        }
    }
}
