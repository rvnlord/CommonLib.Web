using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace CommonLib.Web.Source.Common.CustomMVCRenderers
{
    public static class LabelHtmlForRenderer
    {
        public static IHtmlContent LabelHtmlFor<TModel, TValue>(this HtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, string template)
        {
            return LabelHtmlFor(html, expression, template, new RouteValueDictionary());
        }

        public static IHtmlContent LabelHtmlFor<TModel, TValue>(this HtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, string template, object htmlAttributes)
        {
            return LabelHtmlFor(html, expression, template, new RouteValueDictionary(htmlAttributes));
        }

        public static IHtmlContent LabelHtmlFor<TModel, TValue>(this HtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, string template, IDictionary<string, object> htmlAttributes)
        {
            if (html == null)
                throw new ArgumentNullException(nameof(html));

            var modelExplorer = html.ViewContext.ViewData.ModelExplorer;
            var metadata = modelExplorer.Metadata;
            var labelName = metadata.DisplayName ?? metadata.PropertyName ?? expression?.ToString().AfterLast(".");
            if (labelName.IsNullOrWhiteSpace())
                return HtmlString.Empty;

            var labelHtml = template.ReplaceInvariant("{prop}", labelName);

            var tag = new TagBuilder("label");
            tag.Attributes.Add("for", html.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldId(html, labelName));
            tag.MergeAttributes(htmlAttributes, true);
            tag.InnerHtml.SetHtmlContent(labelHtml);

            return tag;
        }

    }
}