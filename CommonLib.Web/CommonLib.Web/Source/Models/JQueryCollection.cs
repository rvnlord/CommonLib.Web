using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Converters;
using CommonLib.Web.Source.Services;
using CommonLib.Web.Source.Services.Interfaces;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils.UtilClasses;
using Microsoft.JSInterop;

namespace CommonLib.Web.Source.Models
{
    public class JQueryCollection : CustomList<JQuery>
    {
        public IJSRuntime JsRuntime { get; set; }
        public IJQuery JQueryService { get; set; }

        public JQueryCollection(IJQuery jqueryService)
        {
            if (jqueryService == null)
                return;

            JQueryService = jqueryService;
            JsRuntime = jqueryService.JsRuntime;
        }

        public JQueryCollection(IEnumerable<JQuery> domElements, IJQuery jqueryService)
        {
            JQueryService = jqueryService;
            JsRuntime = (jqueryService ?? throw new NullReferenceException(nameof(jqueryService))).JsRuntime;
            _customList.AddRange(domElements);
        }

        public ValueTask<JQuery> FirstAsync()
        {
            return _customList.ToAsyncEnumerable().FirstAsync();
        }

        public ValueTask<JQuery> FirstAsync(Func<JQuery, ValueTask<bool>> selector)
        {
            return _customList.ToAsyncEnumerable().FirstAwaitAsync(selector);
        }

        public async Task<JQueryCollection> WhereAsync(Func<JQuery, ValueTask<bool>> selector)
        {
            return await _customList.ToAsyncEnumerable().WhereAwait(selector).ToJQueryCollectionAsync(JQueryService).ConfigureAwait(false);
        }

        public Task<JQueryCollection> WhereAsync(string filterSelector) => FilterAsync(filterSelector);

        public Task<JQueryCollection> FilterAsync(Func<JQuery, ValueTask<bool>> selector) => WhereAsync(selector);

        public async Task<JQueryCollection> FilterAsync(string filterSelector)
        {
            return (await JsRuntime.InvokeAsync<string>("BlazorJQueryUtils.Filter", GetSelector(), filterSelector).ConfigureAwait(false))
                .JsonDeserialize().ToJQueryCollection(JQueryService).OrderByAnother(jq => jq.Guid, this).ToJQueryCollection(JQueryService);
        }

        public async Task<IEnumerable<T>> SelectAsync<T>(Func<JQuery, ValueTask<T>> select)
        {
            return await _customList.ToAsyncEnumerable().SelectAwait(select).ToListAsync().ConfigureAwait(false);
        }

        public async Task<JQueryCollection> NotAsync(string notSelector)
        {
            return (await JsRuntime.InvokeAsync<string>("BlazorJQueryUtils.Not", GetSelector(), notSelector).ConfigureAwait(false))
                .JsonDeserialize().ToJQueryCollection(JQueryService);
        }

        public async Task<JQueryCollection> NotAsync(JQuery notElement)
        {
            return (await JsRuntime.InvokeAsync<string>("BlazorJQueryUtils.Not", GetSelector(), 
                    (notElement ?? throw new NullReferenceException(nameof(notElement))).GetSelector()).ConfigureAwait(false))
                .JsonDeserialize().ToJQueryCollection(JQueryService);
        }

        public async Task<JQueryCollection> NotAsync(JQueryCollection notElements)
        {
            return (await JsRuntime.InvokeAsync<string>("BlazorJQueryUtils.Not", GetSelector(), 
                    (notElements ?? throw new NullReferenceException(nameof(notElements))).GetSelector()).ConfigureAwait(false))
                .JsonDeserialize().ToJQueryCollection(JQueryService);
        }

        public async Task<JQueryCollection> RemoveClassAsync(string classToRemoveFromAll)
        {
            await JsRuntime.InvokeVoidAsync("BlazorJQueryUtils.RemoveClass", GetSelector(), classToRemoveFromAll).ConfigureAwait(false);
            foreach (var jq in _customList.Where(jq => !jq.Classes.Contains(classToRemoveFromAll)))
                jq.Classes.Remove(classToRemoveFromAll);
            return this;
        }

        public async Task<JQueryCollection> ChildrenAsync()
        {
            return (await JsRuntime.InvokeAsync<string>("BlazorJQueryUtils.Children", GetSelector()).ConfigureAwait(false))
                .JsonDeserialize().ToJQueryCollection(JQueryService);
        }

        public async Task<JQueryCollection> ChildrenAsync(string selector)
        {
            return (await JsRuntime.InvokeAsync<string>("BlazorJQueryUtils.ChildrenBySelector", GetSelector(), selector).ConfigureAwait(false))
                .JsonDeserialize().ToJQueryCollection(JQueryService);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(_customList.Take(3).Select(jq => $"{{ {jq.ToShortString()} }}").JoinAsString(", "));
            if (_customList.Count > 3)
                sb.Append($" ... {{ {_customList.Last().ToShortString()} }}");

            return sb.ToString();
        }

        public string GetSelector() => _customList.Select(jq => $"[my-guid='{jq.Guid}']").JoinAsString(", ");
    }
}
