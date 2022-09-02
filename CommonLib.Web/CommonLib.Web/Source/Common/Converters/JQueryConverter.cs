using System;
using System.Linq;
using CommonLib.Web.Source.Models;
using CommonLib.Web.Source.Services;
using CommonLib.Web.Source.Services.Interfaces;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using Newtonsoft.Json.Linq;

namespace CommonLib.Web.Source.Common.Converters
{
    public static class JQueryConverter
    {
        public static Models.JQuery ToJQuery(this JToken jt, IJQueryService jqueryService)
        {
            if (jt == null)
                throw new NullReferenceException(nameof(jt));

            if (jt is JArray)
                jt = jt.ToJArray()[0];

            //if (jqueryService == null)
            //{
            //    var jqueryServiceDescriptor = WebUtils.Services.Single(s => s.ServiceType == typeof(IJQueryService));
            //    jqueryService = (IJQueryService) Activator.CreateInstance(jqueryServiceDescriptor.ImplementationType, WebUtils.JsRuntime);
            //}

            var jquery = new Models.JQuery(jqueryService, Guid.Parse(jt["guid"].ToStringInvariant()))
            {
                Id = jt["id"].ToStringInvariant().NullifyIf(s => s.IsNullOrWhiteSpace()),
                Name = jt["name"].ToStringInvariant().NullifyIf(s => s.IsNullOrWhiteSpace()),
                Text = jt["text"].ToStringInvariant().NullifyIf(s => s.IsNullOrWhiteSpace()),
                Value = jt["value"].ToStringInvariant().NullifyIf(s => s.IsNullOrWhiteSpace()),
                OriginalSelector = jt["originalSelector"].ToStringInvariant().NullifyIf(s => s.IsNullOrWhiteSpace())
            };

            jquery.Classes.ReplaceAll(jt["classes"].Select(c => c.ToStringInvariant()).ToList());
            return jquery;
        }

        //public static Task<JQuery> ToJQueryAsync(this Task<JToken> jt, IJQueryService jqueryService)
        //{

        //}
    }
}
