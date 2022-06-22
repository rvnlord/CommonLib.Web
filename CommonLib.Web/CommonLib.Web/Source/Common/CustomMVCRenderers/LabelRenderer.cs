using System;
using System.Linq.Expressions;
using CommonLib.Source.Common.Extensions;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace CommonLib.Web.Source.Common.CustomMVCRenderers
{
    public static class LabelRenderer
    {
        public static IHtmlContent CustomLabelFor<TModel, TValue>(this HtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, object htmlAttributes)
        {
            if (html == null)
                throw new ArgumentNullException(nameof(html));

            var me = html.ViewContext.ViewData.ModelExplorer;
            var metadata = me.Metadata;
            var attributes = new RouteValueDictionary(htmlAttributes);
            var labelName = metadata.DisplayName ?? metadata.PropertyName ?? expression?.ToString().AfterLast(".");
            if (labelName.IsNullOrWhiteSpace())
                return HtmlString.Empty;

            var label = new TagBuilder("label");
            label.Attributes.Add("for", TagBuilder.CreateSanitizedId(html.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(labelName), "_"));
            label.InnerHtml.SetContent(labelName);
            label.MergeAttributes(attributes, true);
            return label;
        }
    }
}