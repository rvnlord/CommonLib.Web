using System;
using CommonLib.Source.Common.Converters;

namespace CommonLib.Web.Source.Common.Utils.UtilClasses
{
    public class IconType
    {
        public RegularIconType? RegularIcon { get; }
        public LightIconType? LightIcon { get; }
        public SolidIconType? SolidIcon { get; }
        public BrandsIconType? BrandsIcon { get; }
        public DuotoneIconType? DuotoneIcon { get; }

        public IconType(RegularIconType? regularIcon)
        {
            RegularIcon = regularIcon;
        }

        public IconType(LightIconType? lightIcon)
        {
            LightIcon = lightIcon;
        }

        public IconType(SolidIconType? solidIcon)
        {
            SolidIcon = solidIcon;
        }

        public IconType(BrandsIconType? brandsIcon)
        {
            BrandsIcon = brandsIcon;
        }

        public IconType(DuotoneIconType? duotoneIcon)
        {
            DuotoneIcon = duotoneIcon;
        }

        public static IconType From(RegularIconType regularIcon) => new IconType(regularIcon);
        public static IconType From(LightIconType lightIcon) => new IconType(lightIcon);
        public static IconType From(SolidIconType solidIcon) => new IconType(solidIcon);
        public static IconType From(BrandsIconType brandsIcon) => new IconType(brandsIcon);
        public static IconType From(DuotoneIconType duotoneIcon) => new IconType(duotoneIcon);

        public override string ToString()
        {
            return RegularIcon?.EnumToString() ?? LightIcon?.EnumToString() ?? SolidIcon?.EnumToString() ?? DuotoneIcon?.EnumToString() ?? BrandsIcon?.EnumToString();
        }

        public override bool Equals(object other)
        {
            if (other is not IconType that)
                return false;
            return RegularIcon == that.RegularIcon && LightIcon == that.LightIcon && SolidIcon == that.SolidIcon && BrandsIcon == that.BrandsIcon && DuotoneIcon == that.DuotoneIcon;
        }

        public override int GetHashCode() => HashCode.Combine(RegularIcon, LightIcon, SolidIcon, BrandsIcon, DuotoneIcon);
    }
}
