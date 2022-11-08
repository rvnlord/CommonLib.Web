using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommonLib.Web.Source.Common.Converters;
using CommonLib.Web.Source.Common.Utils;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.UtilClasses;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using MoreLinq;

namespace CommonLib.Web.Source.Common.CustomMVCRenderers
{
    public static class IconRenderer
    {
        private static IconRendererConfig Icon(this IHtmlHelper html, object icon, NavigationManager nav)
        {
            if (html == null)
                throw new ArgumentNullException(nameof(html));

            var iconType = icon.GetType();
            var iconSetDirName = iconType.Name.BeforeFirst("IconType");
            var iconName = StringConverter.PascalCaseToKebabCase(EnumConverter.EnumToString(icon.CastToReflected(iconType)));
            var iconPath = PathUtils.Combine(PathSeparator.FSlash, nav.Uri, $@"Icons/{iconSetDirName}/{iconName}.svg");
            //var iconPath = $@"{WebUtils.GetAbsolutePhysicalContentPath()}\Content\Icons\{iconSetDirName}\{StringConverter.PascalCaseToKebabCase(EnumConverter.EnumToString(icon.CastToReflected(iconType)))}.svg";
            var svg = File.ReadAllText(iconPath).TrimMultiline();

            var divIconContainer = new TagBuilder("div");
            divIconContainer.InnerHtml.SetHtmlContent(svg);
            return new IconRendererConfig(divIconContainer);
        }

        public static IconRendererConfig Icon(this IHtmlHelper html, RegularIconType icon, NavigationManager nav) => html.Icon((object)icon, nav);
        public static IconRendererConfig Icon(this IHtmlHelper html, BrandsIconType icon, NavigationManager nav) => html.Icon((object)icon, nav);
        public static IconRendererConfig Icon(this IHtmlHelper html, DuotoneIconType icon, NavigationManager nav) => html.Icon((object)icon, nav);
        public static IconRendererConfig Icon(this IHtmlHelper html, LightIconType icon, NavigationManager nav) => html.Icon((object)icon, nav);
        public static IconRendererConfig Icon(this IHtmlHelper html, SolidIconType icon, NavigationManager nav) => html.Icon((object)icon, nav);
    }

    public class IconRendererConfig
    {
        private readonly TagBuilder _divIconContainer;

        public IconRendererConfig(TagBuilder divIconContainer)
        {
            _divIconContainer = divIconContainer;
        }

        public IconRendererConfig Class(params string[] classes)
        {
            var classArr = _divIconContainer.Attributes.VorN("class")?.Split(" ").RemoveEmptyEntries() ?? Array.Empty<string>();

            _divIconContainer.MergeAttribute("class", classArr.Concat(classes.Select(c => c.AfterFirst("."))).JoinAsString(" "), true);
            return this;
        }

        public IconRendererConfig Color(string color)
        {
            var svg = _divIconContainer.InnerHtml.ToHtmlAgility().SelectSingleNode("./svg");

            svg.SelectNodes("./path").ForEach(p =>
            {
                var style = p.GetAttributeValue("style", null)?.CssStringToDictionary() ?? new Dictionary<string, string>();
                style["fill"] = color;
                p.SetAttributeValue("style", style.CssDictionaryToString());
            });

            _divIconContainer.InnerHtml.SetHtmlContent(svg.OuterHtml);
            
            return this;
        }

        public IconRendererConfig Size(double width, double height)
        {
            var styleDict = _divIconContainer.Attributes.VorN("style")?.CssStringToDictionary() ?? new Dictionary<string, string>();

            styleDict["width"] = width + "px";
            styleDict["height"] = height + "px";

            var newStyle = styleDict.CssDictionaryToString();

            _divIconContainer.MergeAttribute("style", newStyle, true);
            return this;
        }

        public IHtmlContent Render() => _divIconContainer;
    }


}


