using CommonLib.Source.Common.Extensions;
using Truncon.Collections;

namespace CommonLib.Web.Source.Common.Components
{
    public static class StylesConfig
    {
        public const string BackgroundColor = "#161616";
        public const string SuccessColor = "#008000";
        public const string FailureColor = "#ff5050";
        public const string SuccessBackgroundColor = "#006000";
        public const string FailureBackgroundColor = "#ff2020";
        public const double CommonPadding = 15;
        public const double Gutter = 10;
        public static double HalfGutter => (Gutter / 2).Round(8);
        public const string LineHeightRem = "1.5rem";
        public const double NavBarFontSize = 18;
        //public static RenderFragment FormControlHeight { get; set; } = $"calc({LineHeightRem} + {Gutter.Px()})".ToRenderFragment();
        public static double InputHeight { get; set; } = FontSize * 1.5 + Gutter;
        public const double FontSize = 16;
        public const double LineHeight = 24;
        public const double MaxPageWidth = 1280;

        public static string Font { get; set; } =
            $"font-size: {FontSize}px; " +
            "font-weight: 400;" +
            "font-family: -apple-system, BlinkMacSystemFont, \"Segoe UI\", Roboto, \"Helvetica Neue\", Arial, \"Noto Sans\", sans-serif, \"Apple Color Emoji\", \"Segoe UI Emoji\", \"Segoe UI Symbol\", \"Noto Color Emoji\";";
        
        public static OrderedDictionary<string, double> Devices => new OrderedDictionary<string, double>
        {
            ["xs"] = 0,
            ["sm"] = 576,
            ["md"] = 768,
            ["lg"] = 992,
            ["xl"] = 1200
        };
        public const int ColsNo = 12;
        public const double MenuGutter = 12.5;
        public const int FixedColMaxSize = 1000;
        public const int FixedColStep = 50;

        public static bool AreStylesRendered { get; set; }
        public const string NoOverflowSingleLine = 
            "overflow: hidden;" +
            "text-overflow: ellipsis;" +
            "white-space: nowrap;";
    }
}
