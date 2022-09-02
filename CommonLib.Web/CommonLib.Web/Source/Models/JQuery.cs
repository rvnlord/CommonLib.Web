using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Converters;
using CommonLib.Web.Source.Common.Extensions.Collections;
using CommonLib.Web.Source.Services;
using CommonLib.Web.Source.Services.Interfaces;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using Microsoft.JSInterop;

namespace CommonLib.Web.Source.Models
{
    public class JQuery
    {
        private readonly IJQueryService _jqueryService;
        private readonly IJSRuntime _jsRuntime;
        public Guid Guid { get; }
        public string OriginalSelector { get; set; }
        public List<string> Classes { get; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public string Text { get; set; }

        public JQuery(IJQueryService jqueryService, Guid id)
        {
            _jqueryService = jqueryService ?? throw new NullReferenceException(nameof(jqueryService));
            _jsRuntime = jqueryService.JsRuntime;
            Guid = id;
            Classes = new List<string>();
        }

        public async Task<string> AttrAsync(string attr)
        {
            return await _jsRuntime.InvokeAsync<string>("BlazorJQueryUtils.GetAttr", GetSelector(), attr).ConfigureAwait(false);
        }

        public async Task<JQuery> AttrAsync(string attr, string value)
        {
            await _jsRuntime.InvokeVoidAsync("BlazorJQueryUtils.SetAttr", GetSelector(), attr, value).ConfigureAwait(false);
            return this;
        }

        public async Task<JQuery> RemoveAttrAsync(string attr)
        {
            await _jsRuntime.InvokeVoidAsync("BlazorJQueryUtils.RemoveAttr", GetSelector(), attr).ConfigureAwait(false);
            return this;
        }

        public async Task<string[]> ClassesAsync()
        {
            return (await AttrAsync("class").ConfigureAwait(false))?.Split(" ") ?? Array.Empty<string>();
        }

        public async Task<JQuery> ClassesAsync(string[] classes)
        {
            Classes.ReplaceAll(classes);
            return (await AttrAsync("class", classes.JoinAsString(" ")).ConfigureAwait(false));
        }

        public async Task<JQuery> ClassAsync(string cls)
        {
            Classes.ReplaceAll(new [] { cls });
            return await ClassesAsync(new[] { cls }).ConfigureAwait(false);
        }

        public async Task<JQuery> AddClassAsync(string cls)
        {
            if (!Classes.Contains(cls))
                Classes.Add(cls);
            await _jsRuntime.InvokeVoidAsync("BlazorJQueryUtils.AddClass", GetSelector(), cls).ConfigureAwait(false);
            return this;
        }

        public async Task<string[]> AddClassesAndGetAddedAsync(string[] classesToAdd)
        {
            var addedClasses = (await _jsRuntime.InvokeAsync<string>("BlazorJQueryUtils.AddClassesAndGetAdded", GetSelector(), classesToAdd.JoinAsString(" ")).ConfigureAwait(false)).Split(" ");
            Classes.AddRange(addedClasses);
            return addedClasses;
        }

        public async Task<string> AddClassAndGetAddedAsync(string classToAdd) => (await AddClassesAndGetAddedAsync(new[] { classToAdd }).ConfigureAwait(false)).SingleOrDefault();

        public async Task<string[]> RemoveClassesAndGetRemovedAsync(string[] classesToRemove)
        {
            var removedClasses = (await _jsRuntime.InvokeAsync<string>("BlazorJQueryUtils.RemoveClassesAndGetRemoved", GetSelector(), classesToRemove.JoinAsString(" ")).ConfigureAwait(false)).Split(" ");
            Classes.AddRange(removedClasses);
            return removedClasses;
        }

        public async Task<string> RemoveClassAndGetRemovedAsync(string classToRemove) => (await RemoveClassesAndGetRemovedAsync(new[] { classToRemove }).ConfigureAwait(false)).SingleOrDefault();

        public async Task<JQuery> AddClassesAsync(string[] classes)
        {
            if (classes == null)
                throw new NullReferenceException(nameof(classes));
            foreach (var cls in classes)
                if (!Classes.Contains(cls))
                    Classes.Add(cls);
            await _jsRuntime.InvokeVoidAsync("BlazorJQueryUtils.AddClass", GetSelector(), classes.JoinAsString(" ")).ConfigureAwait(false);
            return this;
        }

        public async Task<JQuery> RemoveClassAsync(string cls)
        {
            if (Classes.Contains(cls))
                Classes.Remove(cls);
            await _jsRuntime.InvokeVoidAsync("BlazorJQueryUtils.RemoveClass", GetSelector(), cls).ConfigureAwait(false);
            return this;
        }

        public async Task<JQuery> RemoveClassesAsync(string[] classes)
        {
            if (classes == null)
                throw new NullReferenceException(nameof(classes));
            foreach (var cls in classes)
                if (Classes.Contains(cls))
                    Classes.Remove(cls);
            await _jsRuntime.InvokeVoidAsync("BlazorJQueryUtils.RemoveClass", GetSelector(), classes.JoinAsString(" ")).ConfigureAwait(false);
            return this;
        }

        public async Task<JQuery> ToggleClassAsync(string cls)
        {
            if (Classes.Contains(cls))
                Classes.Remove(cls);
            else if (!Classes.Contains(cls))
                Classes.Add(cls);
            await _jsRuntime.InvokeVoidAsync("BlazorJQueryUtils.ToggleClass", GetSelector(), cls).ConfigureAwait(false);
            return this;
        }

        public async Task<string> IdAsync()
        {
            return await AttrAsync("id").ConfigureAwait(false);
        }

        public async Task<JQuery> IdAsync(string id)
        {
            Id = id;
            return await AttrAsync("id", id).ConfigureAwait(false);
        }

        public async Task<string> CssAsync(string ruleName)
        {
            return await _jsRuntime.InvokeAsync<string>("BlazorJQueryUtils.GetCss", GetSelector(), ruleName).ConfigureAwait(false);
        }

        public async Task<JQuery> CssAsync(Dictionary<string, string> css)
        {
            await _jsRuntime.InvokeVoidAsync("BlazorJQueryUtils.SetCss", GetSelector(), css).ConfigureAwait(false);
            return this;
        }

        public async Task<JQuery> CssAsync(string ruleName, string ruleValue)
        {
            return await CssAsync(new Dictionary<string, string> { [ruleName] = ruleValue }).ConfigureAwait(false);
        }

        public async Task<JQuery> RemoveCssAsync(string ruleName)
        {
            return (await _jsRuntime.InvokeAsync<string>("BlazorJQueryUtils.RemoveCss", GetSelector(), ruleName).ConfigureAwait(false))
                .JsonDeserialize().ToJQuery(_jqueryService);
        }

        public async Task<JQueryCollection> AncestorsAsync()
        {
            return (await _jsRuntime.InvokeAsync<string>("BlazorJQueryUtils.Parents", GetSelector()).ConfigureAwait(false))
                .JsonDeserialize().ToJQueryCollection(_jqueryService);
        }

        public async Task<JQuery> ClosestAsync(string ancestorSelector) // returns including the current element
        {
            return (await _jsRuntime.InvokeAsync<string>("BlazorJQueryUtils.Closest", GetSelector(), ancestorSelector).ConfigureAwait(false))
                .JsonDeserialize().ToJQuery(_jqueryService);
        }

        public async Task<JQuery> ClosestAsync(Func<JQuery, ValueTask<bool>> selector)
        {
            var ancestors = await AncestorsAsync().ConfigureAwait(false);
            ancestors.Add(this);
            return await ancestors.FirstAsync(selector).ConfigureAwait(false);
        }

        public async Task<JQuery> ClosestAsync(Func<JQuery, bool> selector)
        {
            var ancestors = await AncestorsAsync().ConfigureAwait(false);
            ancestors.Add(this);
            return ancestors.First(selector);
        }

        public async Task<JQuery> AncestorAsync(string ancestorSelector)
        {
            return (await _jsRuntime.InvokeAsync<string>("BlazorJQueryUtils.Closest", GetSelector(), ancestorSelector).ConfigureAwait(false))
                .JsonDeserialize().ToJQuery(_jqueryService);
        }

        public async Task<JQuery> AncestorAsync(Func<JQuery, ValueTask<bool>> selector)
        {
            return await AncestorsAsync().FirstAsync(selector).ConfigureAwait(false);
        }

        public async Task<JQuery> AncestorAsync(Func<JQuery, bool> selector)
        {
            return (await AncestorsAsync().ConfigureAwait(false)).First(selector);
        }
        
        public async Task<JQuery> ParentAsync()
        {
            return (await _jsRuntime.InvokeAsync<string>("BlazorJQueryUtils.Parent", GetSelector()).ConfigureAwait(false))
                .JsonDeserialize().ToJQuery(_jqueryService);
        }

        public async Task<JQueryCollection> ParentsUntilAsync(string parentsUntilSelector)
        {
            return (await _jsRuntime.InvokeAsync<string>("BlazorJQueryUtils.ParentsUntil", GetSelector(), parentsUntilSelector).ConfigureAwait(false))
                .JsonDeserialize().ToJQueryCollection(_jqueryService);
        }

        public async Task<JQueryCollection> ChildrenAsync()
        {
            return (await _jsRuntime.InvokeAsync<string>("BlazorJQueryUtils.Children", GetSelector()).ConfigureAwait(false))
                .JsonDeserialize().ToJQueryCollection(_jqueryService);
        }

        public async Task<JQueryCollection> ChildrenAsync(string selector)
        {
            return (await _jsRuntime.InvokeAsync<string>("BlazorJQueryUtils.ChildrenBySelector", GetSelector(), selector).ConfigureAwait(false))
                .JsonDeserialize().ToJQueryCollection(_jqueryService);
        }

        public async Task<JQueryCollection> ChildrenAsync(Func<JQuery, ValueTask<bool>> selector)
        {
            return await ChildrenAsync().WhereAsync(selector).ConfigureAwait(false);
        }

        public async Task<JQueryCollection> ChildrenAsync(Func<JQuery, bool> selector)
        {
            return (await ChildrenAsync().ConfigureAwait(false)).Where(selector).ToJQueryCollection(_jqueryService);
        }

        public async Task<JQueryCollection> FindAsync(string selector)
        {
            return (await _jsRuntime.InvokeAsync<string>("BlazorJQueryUtils.Find", GetSelector(), selector).ConfigureAwait(false))
                .JsonDeserialize().ToJQueryCollection(_jqueryService);
        }

        public Task<JQueryCollection> DescendantsAsync(string selector) => FindAsync(selector);

        public async Task<JQueryCollection> PrevAllAsync(string prevAllSelector)
        {
            return (await _jsRuntime.InvokeAsync<string>("BlazorJQueryUtils.PrevAll", GetSelector(), prevAllSelector).ConfigureAwait(false))
                .JsonDeserialize().ToJQueryCollection(_jqueryService);
        }

        public async Task<double> WidthAsync()
        {
            return await _jsRuntime.InvokeAsync<double>("BlazorJQueryUtils.Width", GetSelector()).ConfigureAwait(false);
        }

        public async Task<double> OuterWidthAsync()
        {
            return await _jsRuntime.InvokeAsync<double>("BlazorJQueryUtils.OuterWidth", GetSelector()).ConfigureAwait(false);
        }

        public async Task<double> HeightAsync()
        {
            return await _jsRuntime.InvokeAsync<double>("BlazorJQueryUtils.Height", GetSelector()).ConfigureAwait(false);
        }

        public async Task<double> OuterHeightAsync()
        {
            return await _jsRuntime.InvokeAsync<double>("BlazorJQueryUtils.OuterHeight", GetSelector()).ConfigureAwait(false);
        }

        public async Task<bool> IsAsync(string isSelector)
        {
            return await _jsRuntime.InvokeAsync<bool>("BlazorJQueryUtils.Is", GetSelector(), isSelector).ConfigureAwait(false);
        }

        public override string ToString()
        {
            return 
                $"Guid: {Guid}, " +
                $"Id: {Id.ToStringInvariant().VerboseNull()}, " +
                $"Classes: [{(Classes.Any() ? $" {Classes.Select(c => c.ToStringInvariant()).JoinAsString(", ")} " : "")}], " +
                $"Name: {Name.ToStringInvariant().VerboseNull()}, " +
                $"Value: {Value.ToStringInvariant().VerboseNull()}. " +
                $"Text: {Text.ToStringInvariant().VerboseNull()}, " +
                $"Original Selector: {OriginalSelector.ToStringInvariant().VerboseNull()}";
        }

        public string ToShortString()
        {
            return $"Guid: {Guid}";
        }

        public string GetSelector() => $"[my-guid='{Guid}']";
    }
}
