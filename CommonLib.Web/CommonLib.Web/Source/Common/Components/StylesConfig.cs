using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils.TypeUtils;
using CommonLib.Web.Source.Common.Extensions;
using Truncon.Collections;

namespace CommonLib.Web.Source.Common.Components
{
    public static class StylesConfig
    {
        public const string BackgroundColor = "#161616";
        public const string SuccessColor = "#008000";
        public const string FailureColor = "#ff5050";
        public const string SuccessBackgroundColor = "#006000";
        public const string ErrorBackgroundColor = "#ff2020";
        public const string WarningBackgroundColor = "#aaaa00";
        public const string PrimaryBackgroundColor = "#000080";
        public const string InfoBackgroundColor = "#027c80";
        public const string InputBackgroundColor = "#303030";
        public const string InputDisabledBackgroundColor = "#101010";
        public const string SuccessBackgroundGradient = $"linear-gradient(to bottom, {SuccessBackgroundColor}, #000000)";
        public const string ErrorBackgroundGradient = $"linear-gradient(to bottom, {ErrorBackgroundColor}, #000000)";
        public const string WarningBackgroundGradient = $"linear-gradient(to bottom, {WarningBackgroundColor}, #000000)";
        public const string PrimaryBackgroundGradient = $"linear-gradient(to bottom, {PrimaryBackgroundColor}, #000000)";
        public const string InfoBackgroundGradient = $"linear-gradient(to bottom, {InfoBackgroundColor}, #000000)";
        public const string InputBackgroundGradient = $"linear-gradient(to bottom, {InputBackgroundColor}, #000000);";
        public const string InputDisabledBackgroundGradient = $"linear-gradient(to bottom, {InputDisabledBackgroundColor}, #000000);";
        public const double CommonPadding = 10;
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
            $"font-size: {FontSize}px; "
            + "font-weight: 400;"
            + "font-family: -apple-system, BlinkMacSystemFont, \"Segoe UI\", Roboto, \"Helvetica Neue\", Arial, \"Noto Sans\", sans-serif, \"Apple Color Emoji\", \"Segoe UI Emoji\", \"Segoe UI Symbol\", \"Noto Color Emoji\";";

        public static OrderedDictionary<string, double> DeviceSizeKindNamesWithSizes =>
            new()
            {
                ["xs"] = 0,
                ["sm"] = 576,
                ["md"] = 768,
                ["lg"] = 992,
                ["xl"] = 1200
            };

        public static OrderedDictionary<string, string> DeviceSizeKindNamesWithMediaQueries =>
            EnumUtils.GetValues<DeviceSizeKind>().ToOrderedDictionary(d => d.EnumToString().ToLower(), d => d.ToMediaQuery());

        public const int ColsNo = 12;
        public const double MenuGutter = 12.5;
        public const int FixedColMaxSize = 1000;
        public const int FixedColStep = 50;

        public static bool AreStylesRendered { get; set; }
        public const string NoOverflowSingleLine = "overflow: hidden;" + "text-overflow: ellipsis;" + "white-space: nowrap;";
    }

    public enum DeviceSizeKind
    {
        XS,
        SM,
        MD,
        LG,
        XL
    }
}
