using System;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace CommonLib.Web.Source.Common.Extensions
{
    public static class TemplateInfoExtensions
    {
        public static string GetFullHtmlFieldId<TModel>(this TemplateInfo ti, IHtmlHelper<TModel> html, string partialFieldName)
        {
            if (html == null)
                throw new ArgumentNullException(nameof(html));
            if (ti == null)
                throw new ArgumentNullException(nameof(ti));

            return html.GenerateIdFromName(ti.GetFullHtmlFieldName(partialFieldName));
        }
    }
}
