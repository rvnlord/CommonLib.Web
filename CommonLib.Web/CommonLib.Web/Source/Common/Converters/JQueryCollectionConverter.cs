using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonLib.Web.Source.Models;
using CommonLib.Web.Source.Services;
using CommonLib.Web.Source.Services.Interfaces;
using CommonLib.Source.Common.Converters;
using Newtonsoft.Json.Linq;

namespace CommonLib.Web.Source.Common.Converters
{
    public static class JQueryCollectionConverter
    {
        public static JQueryCollection ToJQueryCollection(this IEnumerable<Models.JQuery> en, IJQueryService jqueryService) => new JQueryCollection(en, jqueryService);
        public static async Task<JQueryCollection> ToJQueryCollectionAsync(this IAsyncEnumerable<Models.JQuery> en, IJQueryService jqueryService) => new JQueryCollection(await en.ToListAsync().ConfigureAwait(false), jqueryService);

        public static JQueryCollection ToJQueryCollection(this JToken jt, IJQueryService jqueryService)
        {
            if (jt == null) 
                throw new ArgumentNullException(nameof(jt));
            if (jqueryService == null) 
                throw new ArgumentNullException(nameof(jqueryService));

            return jt.ToJArray().Select(jjQuery => jjQuery.ToJQuery(jqueryService)).ToJQueryCollection(jqueryService);
        }
    }
}
