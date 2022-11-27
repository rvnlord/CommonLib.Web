using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Components.MyModalComponent;

namespace CommonLib.Web.Source.Common.Extensions
{
    public static class MyModalExtensions
    {
        public static async Task ShowModalAsync(this Task<MyModalBase> taskModal, bool animate = true) => await (await taskModal).ShowModalAsync(animate);
        public static async Task HideModalAsync(this Task<MyModalBase> taskModal, bool animate = true) => await (await taskModal).HideModalAsync(animate);
    }
}
