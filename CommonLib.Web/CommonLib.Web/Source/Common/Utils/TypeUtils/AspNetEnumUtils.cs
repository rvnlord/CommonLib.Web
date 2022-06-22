using System.Collections.Generic;
using System.Linq;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Utils.TypeUtils;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CommonLib.Web.Source.Common.Utils.TypeUtils
{
    public static class AspNetEnumUtils
    {
        public static List<SelectListItem> EnumToSelectListItems<TEnum>()
        {
            return EnumUtils.EnumToDdlItems<TEnum>().Select(di => new SelectListItem(di.Text, di.Index.ToStringInvariant())).ToList();
        }
    }
}
