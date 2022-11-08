using System;
using System.Diagnostics.CodeAnalysis;
using CommonLib.Web.Source.Common.Converters;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Source.Common.Extensions;
using Microsoft.AspNetCore.Components;
using CommonLib.Source.Common.Utils.UtilClasses;

namespace CommonLib.Web.Source.Common.Components.MyIconComponent
{
    [SuppressMessage("Usage", "BL0005:Component parameter should not be set outside of its component.", Justification = "<Pending>")]
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "HTMLHelper like Compatibility")]
    public static class MyIconRenderer
    {
        private static IconRendererConfig MyIcon(object icon)
        {
            if (icon == null)
                throw new ArgumentNullException(nameof(icon));

            var iconComponent = new MyIcon();
            var iconProperty = iconComponent.GetType().GetProperty(icon.GetType().Name.BeforeFirst("Type"));
            iconProperty?.SetValue(iconComponent, icon);
            return new IconRendererConfig(iconComponent);;
        }

        public static IconRendererConfig Icon(this BlazorHtmlHelper html, RegularIconType icon) => MyIcon(icon);
        public static IconRendererConfig Icon(this BlazorHtmlHelper html, BrandsIconType icon) => MyIcon(icon);
        public static IconRendererConfig Icon(this BlazorHtmlHelper html, DuotoneIconType icon) => MyIcon(icon);
        public static IconRendererConfig Icon(this BlazorHtmlHelper html, LightIconType icon) => MyIcon(icon);
        public static IconRendererConfig Icon(this BlazorHtmlHelper html, SolidIconType icon) => MyIcon(icon);
    }

    [SuppressMessage("Usage", "BL0005:Component parameter should not be set outside of its component.", Justification = "<Pending>")]
    public class IconRendererConfig
    {
        private readonly MyIcon _iconComponent;

        public IconRendererConfig(MyIcon iconComponent)
        {
            _iconComponent = iconComponent;
        }

        public IconRendererConfig Color(string color)
        {
            _iconComponent.Color = color;
            return this;
        }

        public RenderFragment Render()
        {
            return _iconComponent.ToRenderFragment(); // 'onclick' render handle is not yet assigned
        }
    }
}
