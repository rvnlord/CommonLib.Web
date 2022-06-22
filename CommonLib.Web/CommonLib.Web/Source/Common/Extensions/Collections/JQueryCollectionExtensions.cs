using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Web.Source.Models;

namespace CommonLib.Web.Source.Common.Extensions.Collections
{
    public static class JQueryCollectionExtensions
    {
        public static async Task<JQuery> FirstAsync(this Task<JQueryCollection> jquery)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).FirstAsync().ConfigureAwait(false);
        }

        public static async Task<JQuery> FirstAsync(this Task<JQueryCollection> jqueryCollection, Func<JQuery, ValueTask<bool>> selector)
        {
            if (jqueryCollection == null)
                throw new NullReferenceException(nameof(jqueryCollection));

            return await (await jqueryCollection.ConfigureAwait(false)).FirstAsync(selector).ConfigureAwait(false);
        }

        public static async Task<JQueryCollection> WhereAsync(this Task<JQueryCollection> jqueryCollection, Func<JQuery, ValueTask<bool>> selector)
        {
            if (jqueryCollection == null)
                throw new NullReferenceException(nameof(jqueryCollection));

            return await (await jqueryCollection.ConfigureAwait(false)).WhereAsync(selector).ConfigureAwait(false);
        }

        public static async Task<JQueryCollection> WhereAsync(this Task<JQueryCollection> jqueryCollection, string selector)
        {
            if (jqueryCollection == null)
                throw new NullReferenceException(nameof(jqueryCollection));

            return await (await jqueryCollection.ConfigureAwait(false)).WhereAsync(selector).ConfigureAwait(false);
        }

        public static async Task<JQueryCollection> FilterAsync(this Task<JQueryCollection> jquery, Func<JQuery, ValueTask<bool>> filterSelector)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).FilterAsync(filterSelector).ConfigureAwait(false);
        }

        public static async Task<JQueryCollection> FilterAsync(this Task<JQueryCollection> jquery, string filterSelector)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).FilterAsync(filterSelector).ConfigureAwait(false);
        }

        public static async Task<IEnumerable<T>> SelectAsync<T>(this Task<JQueryCollection> jquery, Func<JQuery, ValueTask<T>> select)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).SelectAsync(select).ConfigureAwait(false);
        }

        public static async Task<JQueryCollection> NotAsync(this Task<JQueryCollection> jquery, string notSelector)
        {
            return await (await (jquery ?? throw new NullReferenceException(nameof(jquery))).ConfigureAwait(false)).NotAsync(notSelector).ConfigureAwait(false);
        }

        public static async Task<JQueryCollection> NotAsync(this Task<JQueryCollection> jquery, JQuery notElement)
        {
            return await (await (jquery ?? throw new NullReferenceException(nameof(jquery))).ConfigureAwait(false)).NotAsync(notElement).ConfigureAwait(false);
        }

        public static async Task<JQueryCollection> NotAsync(this Task<JQueryCollection> jquery, JQueryCollection notElements)
        {
            return await (await (jquery ?? throw new NullReferenceException(nameof(jquery))).ConfigureAwait(false)).NotAsync(notElements).ConfigureAwait(false);
        }

        public static async Task<JQueryCollection> ChildrenAsync(this Task<JQueryCollection> jquery)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).ChildrenAsync().ConfigureAwait(false);
        }

        public static async Task<JQueryCollection> ChildrenAsync(this Task<JQueryCollection> jquery, string selector)
        {
            if (jquery == null)
                throw new NullReferenceException(nameof(jquery));

            return await (await jquery.ConfigureAwait(false)).ChildrenAsync(selector).ConfigureAwait(false);
        }
    }
}
