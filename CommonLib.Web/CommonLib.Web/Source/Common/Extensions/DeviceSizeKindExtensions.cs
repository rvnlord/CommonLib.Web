using System.Linq;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Utils.TypeUtils;
using CommonLib.Web.Source.Common.Components;

namespace CommonLib.Web.Source.Common.Extensions
{
    public static class DeviceSizeKindExtensions
    {
        public static double GetMinWidth(this DeviceSizeKind deviceSize) => StylesConfig.DeviceSizeKindNamesWithSizes[deviceSize.EnumToString().ToLowerInvariant()];
        public static double? GetMaxWidthOrNull(this DeviceSizeKind deviceSize) => deviceSize.ToInt() + 1 < EnumUtils.GetValues<DeviceSizeKind>().Count() 
            ? StylesConfig.DeviceSizeKindNamesWithSizes[(deviceSize + 1).EnumToString().ToLowerInvariant()] - 0.02
            : null;

        public static string ToMediaQuery(this DeviceSizeKind deviceSize)
        {
            var minWidth = deviceSize.GetMinWidth();
            var maxWidth = deviceSize.GetMaxWidthOrNull();
            return maxWidth is null 
                ? $"(min-width: {minWidth.Px()})" 
                : $"(min-width: {minWidth.Px()}) and (max-width: {maxWidth.Value.Px()})";
        }
    }
}
