using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Components.MyModalComponent;
using Microsoft.JSInterop;

namespace CommonLib.Web.Source.Common.Extensions
{
    public static class MyModalExtensions
    {
        public static async Task ShowModalAsync(this Task<MyModalBase> taskModal)
        {
            var modal = await taskModal;
            await (await modal.ModuleAsync).InvokeVoidAsync("blazor_Modal_ShowAsync", modal.JsModal).ConfigureAwait(false);
        }

        public static async Task HideModalAsync(this Task<MyModalBase> taskModal) 
        {
            var modal = await taskModal;
            await (await modal.ModuleAsync).InvokeVoidAsync("blazor_Modal_HideAsync", modal.JsModal).ConfigureAwait(false);
        }
    }
}
