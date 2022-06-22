using CommonLib.Web.Source.Common.Components;
using CommonLib.Web.Source.Common.Components.MyTitlebarComponent;
using CommonLib.Web.Source.Common.Converters;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CommonLib.Web.Server.Source.Common.Components.MyTitlebarComponent
{
    public static class MyTitlebarRenderer
    {
        public static MyTitlebarRendererConfig MyTitlebar(this BlazorHtmlHelper html)
        {
            return new MyTitlebarRendererConfig(new MyTitlebar());
        }

        public static MyTitlebarRendererConfig MyTitlebar(this IHtmlHelper html)
        {
            return new MyTitlebarRendererConfig(new MyTitlebar());
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "BL0005:Component parameter should not be set outside of its component.", Justification = "<Pending>")]
    public class MyTitlebarRendererConfig
    {
        private readonly MyTitlebar _MyTitlebarComponent;

        public MyTitlebarRendererConfig(MyTitlebar MyTitlebar)
        {
            _MyTitlebarComponent = MyTitlebar;
        }

        public MyTitlebarRendererConfig Title(string title)
        {
            _MyTitlebarComponent.AppTitle = title;
            return this;
        }

        public RenderFragment Render()
        {
            return _MyTitlebarComponent.ToRenderFragment(); // 'onclick' render handle is not yet assigned
        }
    }
}
