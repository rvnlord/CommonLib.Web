using System;
using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Models;

namespace CommonLib.Web.Source.Common.Extensions
{
    public static class BlazorParameterExtensions
    {
        //public static bool HasChangedOrFalse<T>(this BlazorParameter<T> blazorParam) => blazorParam != null && blazorParam.HasChanged();
        //public static bool HasValueOrFalse<T>(this BlazorParameter<T> blazorParam) => blazorParam != null && blazorParam.HasValue();

        //public static bool HasChangedOrDefault<T>(this BlazorParameter<T> blazorParam)
        //{
        //    blazorParam ??= new BlazorParameter<T>(default);
        //    return blazorParam.HasChanged();
        //}

        //public static bool HasValueOrDefault<T>(this BlazorParameter<T> blazorParam)
        //{
        //    blazorParam ??= new BlazorParameter<T>(default);
        //    return blazorParam.HasValue();
        //}

        //public static bool HasPreviousValueOrDefault<T>(this BlazorParameter<T> blazorParam)
        //{
        //    blazorParam ??= new BlazorParameter<T>(default);
        //    return blazorParam.HasPreviousValue();
        //}

        public static void BindValidationStateChanged(this BlazorParameter<MyEditContext> editContextParam, Func<object, MyValidationStateChangedEventArgs, Task> _handleValidationStateChanged)
        {
            if (editContextParam.HasChanged())
            {
                if (editContextParam.HasPreviousValue())
                    editContextParam.PreviousParameterValue.OnValidationStateChangedAsync -= _handleValidationStateChanged;
                if (editContextParam.HasValue())
                    editContextParam.ParameterValue.ReBindValidationStateChanged(_handleValidationStateChanged);
            }
        }
    }
}
