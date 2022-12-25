using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Web.Source.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace CommonLib.Web.Source.Common.Extensions
{
    public static class JQueryExtensions
    {
        public static async Task<string> AttrAsync(this Task<JQuery> jquery, string attr)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).AttrAsync(attr).ConfigureAwait(false);
        }

        public static async Task<JQuery> AttrAsync(this Task<JQuery> jquery, string attr, string value)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).AttrAsync(attr, value).ConfigureAwait(false);
        }
        
        public static async Task<JQuery> RemoveAttrAsync(this Task<JQuery> jquery, string attr)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).RemoveAttrAsync(attr).ConfigureAwait(false);
        }

        public static async Task<string> PropAsync(this Task<JQuery> jquery, string prop)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).PropAsync(prop).ConfigureAwait(false);
        }

        public static async Task<JQuery> PropAsync(this Task<JQuery> jquery, string prop, string value)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).PropAsync(prop, value).ConfigureAwait(false);
        }
        
        public static async Task<string[]> ClassesAsync(this Task<JQuery> jquery)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).ClassesAsync().ConfigureAwait(false);
        }

        public static async Task<JQuery> ClassesAsync(this Task<JQuery> jquery, string[] classes)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).ClassesAsync(classes).ConfigureAwait(false);
        }

        public static async Task<JQuery> ClassAsync(this Task<JQuery> jquery, string cls)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).ClassAsync(cls).ConfigureAwait(false);
        }

        public static async Task<JQuery> AddClassAsync(this Task<JQuery> jquery, string cls)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).AddClassAsync(cls).ConfigureAwait(false);
        }

        public static async Task<JQuery> AddClassesAsync(this Task<JQuery> jquery, string[] classes)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).AddClassesAsync(classes).ConfigureAwait(false);
        }

        public static async Task<JQuery> RemoveClassAsync(this Task<JQuery> jquery, string cls)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).RemoveClassAsync(cls).ConfigureAwait(false);
        }

        public static async Task<JQuery> RemoveClassesAsync(this Task<JQuery> jquery, string[] classes)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).RemoveClassesAsync(classes).ConfigureAwait(false);
        }

        public static async Task<string[]> AddClassesAndGetAddedAsync(this Task<JQuery> jquery, string[] classesToAdd)
        {
            return await (await (jquery ?? throw new NullReferenceException(nameof(jquery))).ConfigureAwait(false))
                .AddClassesAndGetAddedAsync(classesToAdd).ConfigureAwait(false);
        }

        public static async Task<string> AddClassAndGetAddedAsync(this Task<JQuery> jquery, string classToAdd)
        {
            return await (await (jquery ?? throw new NullReferenceException(nameof(jquery))).ConfigureAwait(false))
                .AddClassAndGetAddedAsync(classToAdd).ConfigureAwait(false);
        }

        public static async Task<string[]> RemoveClassesAndGetRemovedAsync(this Task<JQuery> jquery, string[] classesToRemove)
        {
            return await (await (jquery ?? throw new NullReferenceException(nameof(jquery))).ConfigureAwait(false))
                .RemoveClassesAndGetRemovedAsync(classesToRemove).ConfigureAwait(false);
        }

        public static async Task<string> RemoveClassAndGetRemovedAsync(this Task<JQuery> jquery, string classToRemove)
        {
            return await (await (jquery ?? throw new NullReferenceException(nameof(jquery))).ConfigureAwait(false))
                .RemoveClassAndGetRemovedAsync(classToRemove).ConfigureAwait(false);
        }

        public static async Task<JQuery> ReplaceClassesAsync(this Task<JQuery> jquery, string[] classes)
        {
            return await (await (jquery ?? throw new NullReferenceException(nameof(jquery))).ConfigureAwait(false))
                .ReplaceClassesAsync(classes).ConfigureAwait(false);
        }

        public static async Task<JQuery> ReplaceClassesAsync(this Task<JQuery> jquery, string cls)
        {
            return await (await (jquery ?? throw new NullReferenceException(nameof(jquery))).ConfigureAwait(false))
                .ReplaceClassesAsync(cls).ConfigureAwait(false);
        }

        public static async Task<JQuery> ToggleClassAsync(this Task<JQuery> jquery, string cls)
        {
            return await (await (jquery ?? throw new NullReferenceException(nameof(jquery))).ConfigureAwait(false)).ToggleClassAsync(cls).ConfigureAwait(false);
        }

        public static async Task<JQuery> IdAsync(this Task<JQuery> jquery, string id)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).IdAsync(id).ConfigureAwait(false);
        }

        public static async Task<string> IdAsync(this Task<JQuery> jquery)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).IdAsync().ConfigureAwait(false);
        }

        public static async Task<string> CssAsync(this Task<JQuery> jquery, string ruleName)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).CssAsync(ruleName).ConfigureAwait(false);
        }

        public static async Task<JQuery> CssAsync(this Task<JQuery> jquery, Dictionary<string, string> css)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).CssAsync(css).ConfigureAwait(false);
        }

        public static async Task<JQuery> CssAsync(this Task<JQuery> jquery, string ruleName, string ruleValue)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).CssAsync(ruleName, ruleValue).ConfigureAwait(false);
        }
        
        public static async Task<JQuery> RemoveCssAsync(this Task<JQuery> jquery, string ruleName)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).RemoveCssAsync(ruleName).ConfigureAwait(false);
        }

        public static async Task<JQuery> AncestorAsync(this Task<JQuery> jquery, string selector)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).AncestorAsync(selector).ConfigureAwait(false);
        }

        public static async Task<JQuery> AncestorAsync(this Task<JQuery> jquery, Func<JQuery, ValueTask<bool>> selector)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).AncestorAsync(selector).ConfigureAwait(false);
        }

        public static async Task<JQuery> AncestorAsync(this Task<JQuery> jquery, Func<JQuery, bool> selector)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).AncestorAsync(selector).ConfigureAwait(false);
        }

        public static async Task<JQuery> ClosestAsync(this Task<JQuery> jquery, string selector)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).ClosestAsync(selector).ConfigureAwait(false);
        }

        public static async Task<JQuery> ClosestAsync(this Task<JQuery> jquery, Func<JQuery, ValueTask<bool>> selector)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).ClosestAsync(selector).ConfigureAwait(false);
        }

        public static async Task<JQuery> ClosestAsync(this Task<JQuery> jquery, Func<JQuery, bool> selector)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).ClosestAsync(selector).ConfigureAwait(false);
        }

        public static async Task<JQuery> ParentAsync(this Task<JQuery> jquery)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).ParentAsync().ConfigureAwait(false);
        }

        public static async Task<JQueryCollection> ParentsUntilAsync(this Task<JQuery> jquery, string parentsUntilSelector)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).ParentsUntilAsync(parentsUntilSelector).ConfigureAwait(false);
        }

        public static async Task<JQueryCollection> ChildrenAsync(this Task<JQuery> jquery)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).ChildrenAsync().ConfigureAwait(false);
        }

        public static async Task<JQueryCollection> ChildrenAsync(this Task<JQuery> jquery, string selector)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).ChildrenAsync(selector).ConfigureAwait(false);
        }

        public static async Task<JQueryCollection> ChildrenAsync(this Task<JQuery> jquery, Func<JQuery, ValueTask<bool>> selector)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).ChildrenAsync(selector).ConfigureAwait(false);
        }

        public static async Task<JQueryCollection> ChildrenAsync(this Task<JQuery> jquery, Func<JQuery, bool> selector)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).ChildrenAsync(selector).ConfigureAwait(false);
        }

        public static async Task<JQueryCollection> FindAsync(this Task<JQuery> jquery, string selector)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).FindAsync(selector).ConfigureAwait(false);
        }

        public static async Task<JQueryCollection> DescendantsAsync(this Task<JQuery> jquery, string selector)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).DescendantsAsync(selector).ConfigureAwait(false);
        }

        public static async Task<JQueryCollection> PrevAllAsync(this Task<JQuery> jquery, string prevAllSelector)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).PrevAllAsync(prevAllSelector).ConfigureAwait(false);
        }

        public static async Task<double> WidthAsync(this Task<JQuery> jquery)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).WidthAsync().ConfigureAwait(false);
        }

        public static async Task<double> OuterWidthAsync(this Task<JQuery> jquery)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).OuterWidthAsync().ConfigureAwait(false);
        }

        public static async Task<double> HeightAsync(this Task<JQuery> jquery)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).HeightAsync().ConfigureAwait(false);
        }

        public static async Task<double> OuterHeightAsync(this Task<JQuery> jquery)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).OuterHeightAsync().ConfigureAwait(false);
        }

        public static async Task<bool> IsAsync(this Task<JQuery> jquery, string isSelector)
        {
            return await (await (jquery ?? throw new NullReferenceException(nameof(jquery))).ConfigureAwait(false)).IsAsync(isSelector).ConfigureAwait(false);
        }

        public static async Task<int> CaretPositionAsync(this Task<JQuery> jquery)
        {
            return await (await (jquery ?? throw new NullReferenceException(nameof(jquery))).ConfigureAwait(false)).CaretPositionAsync().ConfigureAwait(false);
        }

        public static async Task<int> CaretPositionAsync(this Task<JQuery> jquery, int caretPosition)
        {
            return await (await (jquery ?? throw new NullReferenceException(nameof(jquery))).ConfigureAwait(false)).CaretPositionAsync(caretPosition).ConfigureAwait(false);
        }
    }
}
