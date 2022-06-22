using CommonLib.Web.Source.Common.Components;
using CommonLib.Web.Source.Common.Components.MyTitlebarComponent;
using CommonLib.Web.Source.Common.Converters;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CommonLib.Web.Server.Source.Common.Components.MyTitlebarComponent
{
    public static class MyTitlebarStylesRenderer
    {
        public static MyTitlebarStylesRendererConfig MyTitlebarStyles(this BlazorHtmlHelper html)
        {
            return new MyTitlebarStylesRendererConfig(new MyTitlebarStyles());
        }

        public static MyTitlebarStylesRendererConfig MyTitlebarStyles(this IHtmlHelper html)
        {
            return new MyTitlebarStylesRendererConfig(new MyTitlebarStyles());
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "BL0005:Component parameter should not be set outside of its component.", Justification = "<Pending>")]
    public class MyTitlebarStylesRendererConfig
    {
        private readonly MyTitlebarStyles _MyTitlebarStylesComponent;

        public MyTitlebarStylesRendererConfig(MyTitlebarStyles MyTitlebarStyles)
        {
            _MyTitlebarStylesComponent = MyTitlebarStyles;
        }

        public RenderFragment Render()
        {
            return _MyTitlebarStylesComponent.ToRenderFragment();
        }
    }
}