using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils.UtilClasses;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using MoreLinq;

namespace CommonLib.Web.Source.Common.CustomMVCRenderers
{
    public static class BootstrapDropDownForRenderer
    {
        public static IHtmlContent BootstrapDropDownFor<TModel, TValue>(
            this IHtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, 
            List<SelectListItem> selectListItems, 
            object htmlAttributes)
        {
            if (html == null)
                throw new ArgumentNullException(nameof(html));

            var modelExplorer = html.ViewContext.ViewData.ModelExplorer;
            var model = (TModel) modelExplorer.Model;
            var propertyName = model.GetPropertyName(expression);
            var displayName = model.GetPropertyDisplayName(expression) ?? propertyName;
            if (displayName.IsNullOrWhiteSpace())
                return HtmlString.Empty;
            
            var attributes = new RouteValueDictionary(htmlAttributes);
            var selectedValue = model.GetPropertyValue(expression)?.ToString() ?? attributes["value"]?.ToString() ?? "-1";

            var id = attributes["id"]?.ToString() ?? "input" + propertyName;
            var name = attributes["name"]?.ToString() ?? propertyName;
            var button = new TagBuilder("button")
            {
                Attributes =
                {
                    { "type", "button" },
                    { "data-toggle", "dropdown" }
                }
            };

            var classes = attributes["class"]?.ToString().Split(" ").RemoveEmptyEntries().Reverse().ToArray();
            if (classes != null) // classes are added in inverted priority so I am reversing the array
                foreach (var c in classes)
                    button.AddCssClass(c);

            if (attributes.ContainsKey("class"))
                attributes.Remove("class");

            button.MergeAttributes(attributes, true);
            button.InnerHtml.SetHtmlContent(BuildDescription(displayName, id, name, selectedValue, selectListItems));

            var div = new TagBuilder("div");
            div.AddCssClass("dropdown");
            div.InnerHtml.AppendHtml(button);
            div.InnerHtml.AppendHtml(BuildDropdown(selectListItems));

            return div;
        }

        public static IHtmlContent BuildDescription(string displayName, string id, string name, string selectedValue, IEnumerable<SelectListItem> items)
        {
            var hiddenInput = new TagBuilder("input")
            {
                Attributes =
                {
                    ["id"] = id,
                    ["name"] = name,
                    ["type"] = "hidden",
                    ["value"] = selectedValue
                }
            };
            hiddenInput.AddCssClass("dropdown-hiddeninput");

            var text = items.SingleOrDefault(i => i.Value == selectedValue)?.Text;

            var span = new TagBuilder("label");
            span.AddCssClass("dropdown-description");
            span.InnerHtml.SetContent(text ?? $"(Select {displayName})");

            return hiddenInput.InnerHtml.AppendHtml(span);
        }

        public static IHtmlContent BuildDropdown(IEnumerable<SelectListItem> items)
        {
            var divListContainer = new TagBuilder("div");

            divListContainer.AddCssClass("dropdown-menu");
            items.ForEach(x => divListContainer.InnerHtml.AppendHtml(BuildListRow(x)));

            return divListContainer;
        }

        private static IHtmlContent BuildListRow(SelectListItem item)
        {
            var a = new TagBuilder("a")
            {
                Attributes =
                {
                    { "tabindex", item.Value },
                    { "value", item.Value },
                }
            };

            a.InnerHtml.SetContent(item.Text);
            a.AddCssClass("dropdown-item");

            return a;
        }
    }
}