using System;
using System.Threading.Tasks;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils.UtilClasses;
using Microsoft.JSInterop;

namespace CommonLib.Web.Source.Common.Extensions
{
    public static class IJSRuntimeExtensions
    {
        public static async Task<IJSObjectReference> ImportModuleAndRetryIfCancelledAsync(this IJSRuntime jsRuntime, string modulePath)
        {
            var i = 1;
            while (i <= 10)
            {
                try
                {
                    if (jsRuntime.GetProperty<bool>("IsInitialized") == false)
                        return null;

                    return await jsRuntime.InvokeAsync<IJSObjectReference>("import", modulePath).AsTask();
                }
                catch (TaskCanceledException ex)
                {
                    if (i >= 10)
                    {
                        Logger.For(typeof(IJSRuntimeExtensions)).Error("Unable to import module: " + ex.Message);
                        throw;
                    }

                    i++;
                }
            }
            
            throw new NotSupportedException("Module not importeed, error not thrown - it shouldn't happen");
        }

        public static async ValueTask InvokeAndCatchCancellationAsync(this IJSRuntime jsRuntime, string identifier, params object[] args)
        {
            try
            {
                await jsRuntime.InvokeVoidAsync(identifier, args);
            }
            catch (TaskCanceledException) { }
        }

        public static ValueTask<TValue> InvokeAndCatchCancellationAsync<TValue>(this IJSRuntime jsRuntime, string identifier, params object[] args)
        {
            try
            {
                return jsRuntime.InvokeAsync<TValue>(identifier, args);
            }
            catch (TaskCanceledException)
            {
                return new ValueTask<TValue>();
            }
        }

        public static async ValueTask ForceInvokeVoidAsync(this IJSRuntime jsRuntime, string identifier, params object[] args)
        {
            var i = 1;
            while (i <= 30)
            {
                try
                {
                    if (jsRuntime.GetProperty<bool>("IsInitialized") == false)
                        return;

                    await jsRuntime.InvokeVoidAsync(identifier, TimeSpan.FromSeconds(1), args).AsTask();
                    return;
                }
                catch (TaskCanceledException ex)
                {
                    if (i >= 30)
                    {
                        Logger.For(typeof(IJSRuntimeExtensions)).Error("Unable to execute method: " + ex.Message);
                        throw;
                    }

                    i++;
                }
            }
            
            throw new NotSupportedException("Module not imported, error not thrown - it shouldn't happen");
        }

        public static async ValueTask<TValue> ForceInvokeAsync<TValue>(this IJSRuntime jsRuntime, string identifier, params object[] args)
        {
            var i = 1;
            while (i <= 30)
            {
                try
                {
                    if (jsRuntime.GetProperty<bool>("IsInitialized") == false)
                        return (TValue)(object)null;

                    return await jsRuntime.InvokeAsync<TValue>(identifier, TimeSpan.FromSeconds(1), args).AsTask();
                }
                catch (TaskCanceledException ex)
                {
                    if (i >= 30)
                    {
                        Logger.For(typeof(IJSRuntimeExtensions)).Error("Unable to execute method: " + ex.Message);
                        throw;
                    }

                    i++;
                }
            }
            
            throw new NotSupportedException("Module not imported, error not thrown - it shouldn't happen");
        }
    }
}
