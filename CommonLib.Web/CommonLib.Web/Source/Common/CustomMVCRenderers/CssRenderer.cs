using System;
using System.Linq;
using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Converters;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using ExCSS;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CommonLib.Web.Source.Common.CustomMVCRenderers
{
    public static class CssRenderer
    {
        public static async Task<CssRendererSaveConfig> ExtractCssAsync(this string strHtmlContent)
        {
            return await Task.FromResult(strHtmlContent).ExtractCss();
        }

        public static async Task<CssRendererSaveConfig> ExtractCss(this Task<string> strHtmlContent)
        {
            if (strHtmlContent == null)
                throw new ArgumentNullException(nameof(strHtmlContent));
            var strHtml = (await strHtmlContent).RemoveMany(@"\x3C!--!-->", "<!--!-->").Replace("}@", "} @").RegexReplace(@"\s+", " ");
            var mvcHtml = new HtmlString(strHtml);
            var agilityHtml = mvcHtml.ToHtmlAgility();
            var strCss = agilityHtml.SelectNodes("./style").Select(n => n.InnerHtml).Aggregate((css1, css2) => $"{css1}\n{css2}"); // ExCss doesn't support calc() 
            var styles = strCss.ToExCss(); 

            return new CssRendererSaveConfig(styles);
        }

        public static Task<IHtmlContent> CssReference(this IHtmlHelper html, string path)
        {
            return Task.FromResult(new CssRendererReferenceConfig(path).Reference().Render());
        }
    }

    public class CssRendererSaveConfig
    {
        public Stylesheet Styles { get; }

        public CssRendererSaveConfig(Stylesheet styles)
        {
            Styles = styles;
        }
    }

    public class CssRendererReferenceConfig
    {
        private readonly string _path;

        public CssRendererReferenceConfig(string path)
        {
            _path = path;
        }

        public CssRendererRenderConfig Reference()
        {
            return new CssRendererRenderConfig($@"<link href=""{_path.AfterFirst(@"~/wwwroot/")}"" rel=""stylesheet"" />");
        }
    }

    public static class CssRendererReference
    {
        public static async Task<CssRendererRenderConfig> Reference(this Task<CssRendererReferenceConfig> config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            return (await config.ConfigureAwait(false)).Reference();
        }
    }

    public class CssRendererRenderConfig
    {
        private readonly string _cssReference;

        public CssRendererRenderConfig(string cssReference)
        {
            _cssReference = cssReference;
        }

        public IHtmlContent Render()
        {
            return new HtmlString(_cssReference);
        }
    }

    public static class CssRendererRender
    {
        public static async Task<IHtmlContent> Render(this Task<CssRendererRenderConfig> config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            return (await config.ConfigureAwait(false)).Render();
        }
    }
}

// @await Html.PartialAsync("_Styles").ExtractCss().SaveAs("~/wwwroot/Styles/Styles.css").Reference().Render()
