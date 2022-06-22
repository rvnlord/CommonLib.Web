using System;
using CommonLib.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace CommonLib.Web.Source.Common.CustomMVCRenderers
{
    public static class ImageRenderer
    {
        public static IHtmlContent Image(this HtmlHelper html, string src, string alt)
        {
            if (html == null)
                throw new ArgumentNullException(nameof(html));

            var img = new TagBuilder("img");

            var path = src;
            if (path.Contains("://"))
                path = src.AfterFirst("://").AfterFirstOrWhole("/");

            img.Attributes.Add("src", path);
            img.Attributes.Add("alt", alt);
            return img;
        }
    }
}