using System.Threading.Tasks;
using CommonLib.Source.Common.Extensions;
using Microsoft.JSInterop;

namespace CommonLib.Web.Source.Common.Extensions
{
    public static class IJSRuntimeExtensions
    {
        public static async Task<IJSObjectReference> ImportModuleAndRetryIfCancelledAsync(this IJSRuntime jsRuntime, string modulePath)
        {
            while (true)
            {
                try
                {
                    if (jsRuntime.GetProperty<bool>("IsInitialized") == false)
                        return null;

                    return await jsRuntime.InvokeAsync<IJSObjectReference>("import", modulePath).AsTask();
                }
                catch (TaskCanceledException) { }
            } 
        }
    }
}
