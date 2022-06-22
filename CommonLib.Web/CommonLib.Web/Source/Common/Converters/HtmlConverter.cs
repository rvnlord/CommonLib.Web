using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Web;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Components;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Html;

namespace CommonLib.Web.Source.Common.Converters
{
    public static class HtmlConverter
    {
        public static string IHtmlContentToString(this IHtmlContent htmlBuilder)
        {
            if (htmlBuilder == null)
                throw new ArgumentNullException(nameof(htmlBuilder));

            var sw = new StringWriter();
            htmlBuilder.WriteTo(sw, HtmlEncoder.Default);
            var htmlStr = HttpUtility.HtmlDecode(sw.ToString());
            sw.Dispose();
            return htmlStr;
        }

        public static HtmlNode ToHtmlAgility(this IHtmlContent htmlBuilder)
        {
            return htmlBuilder.IHtmlContentToString().HTML().Root();
        }

        public static RenderFragment ToRenderFragment(this string str) => builder => builder.AddMarkupContent(0, str);
        public static RenderFragment ToRenderFragment(this HtmlNode html) => html.HtmlAgilityToString().ToRenderFragment();
        public static RenderFragment ToRenderFragment<TComponent>(this TComponent component) where TComponent : MyComponentBase
        {
            component.OnParametersSetAsync().Sync();
            return component.BuildRenderTree;
            //return (RenderFragment) component?.GetType().BaseType?.GetField("_renderFragment", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(component);
        }

        public static RenderFragment ToRenderFragment(this ComponentBase component)
        {
            component.OnParametersSetAsync().Sync();
            return component.BuildRenderTree;
            //return (RenderFragment) component?.GetType().BaseType?.GetField("_renderFragment", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(component);
        }

        //public static IHtmlContent ToIHtmlContent(this RenderFragment rf) => rf => rf(new HtmlContentBuilder());

    }
}
