using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace CommonLib.Web.Source.Common.CustomMVCRenderers
{
    public static class BootstrapDropDownRenderer
    {
        public static IHtmlContent BootstrapDropDown(this HtmlHelper html, string propertyName, List<SelectListItem> selectListItems, object htmlAttributes)
        {
            if (string.IsNullOrEmpty(propertyName))
                return HtmlString.Empty;

            var attributes = new RouteValueDictionary(htmlAttributes);

            var id = attributes["id"]?.ToString() ?? propertyName;
            var name = attributes["name"]?.ToString() ?? propertyName;
            var selectedValue = attributes["value"]?.ToString() ?? "-1";
            var button = new TagBuilder("button")
            {
                Attributes = 
                {
                    { "type", "button" },
                    { "data-toggle", "dropdown" }
                }
            };

            var classes = attributes["class"]?.ToString().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries).Reverse().ToArray();
            if (classes != null) // classes are added in inverted priority so I am reversing an array
                foreach (var c in classes)
                    button.AddCssClass(c);

            if (attributes.ContainsKey("class"))
                attributes.Remove("class");

            button.MergeAttributes(attributes, true);
            button.InnerHtml.SetHtmlContent(BootstrapDropDownForRenderer.BuildDescription(propertyName, id, name, selectedValue, selectListItems));

            var div = new TagBuilder("div");
            div.AddCssClass("dropdown");
            div.InnerHtml.AppendHtml(button);
            div.InnerHtml.AppendHtml(BootstrapDropDownForRenderer.BuildDropdown(selectListItems));

            return div;
        }
    }
}