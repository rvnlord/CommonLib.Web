using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace CommonLib.Web.Source.Common.Extensions
{
    public static class NavigationManagerExtensions
    {
        public static T GetQueryString<T>(this NavigationManager navManager, string key)
        {
            var uri = navManager.ToAbsoluteUri(navManager.Uri);
            if (!QueryHelpers.ParseQuery(uri.Query).TryGetValue(key, out var valueFromQueryString))
                return (T)(object) null;
            if (typeof(T) == typeof(bool?) && bool.TryParse(valueFromQueryString, out var valueAsBool))
                return (T)(object)valueAsBool;
            if (typeof(T) == typeof(int) && int.TryParse(valueFromQueryString, out var valueAsInt))
                return (T)(object)valueAsInt;
            if (typeof(T) == typeof(string))
                return (T)(object)valueFromQueryString.ToString();
            if (typeof(T) == typeof(decimal) && decimal.TryParse(valueFromQueryString, out var valueAsDecimal))
                return (T)(object)valueAsDecimal;
            return (T)(object)null;
        }
    }
}