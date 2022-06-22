using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Components;

namespace CommonLib.Web.Source.Common.Extensions
{
    public static class MyComponentBaseExtensions
    {
        public static async Task<MyComponentBase> StateHasChangedAsync(this Task<MyComponentBase> myComponent, bool force = false)
        {
            return await (await myComponent).StateHasChangedAsync(force);
        }

        public static async Task<MyComponentBase> WaitForRenderAsync(this Task<MyComponentBase> myComponent)
        {
            return await (await myComponent).WaitForRenderAsync();
        }
    }
}
