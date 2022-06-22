using CommonLib.Web.Source.Models;

namespace CommonLib.Web.Source.Common.Converters
{
    public static class BlazorParameterConverter
    {
        public static BlazorParameter<T> ToBp<T>(this T paramVal) => new(paramVal);
    }
}
