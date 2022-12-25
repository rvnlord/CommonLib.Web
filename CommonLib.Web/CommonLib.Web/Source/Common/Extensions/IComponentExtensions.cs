using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CommonLib.Source.Common.Extensions;
using Microsoft.AspNetCore.Components;

namespace CommonLib.Web.Source.Common.Extensions
{
    public static class IComponentExtensions
    {
        public static void StateHasChanged(this IComponent component)
        {
            component.SetField("_preventRenderStack", 0);
            component.GetType().GetMethod("StateHasChanged", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(component, Array.Empty<object>());
        }

        public static Task StateHasChangedAsync(this IComponent component)
        {
            var baseTypes = component.GetType().GetBaseTypes().ToArray();
            var renderHandle = (RenderHandle) (baseTypes.Single(t => t == typeof(ComponentBase)).GetField("_renderHandle", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(component) ?? throw new NullReferenceException("'_renderHandle' is null"));
            return renderHandle.Dispatcher.InvokeAsync(component.StateHasChanged);
        }

        public static async Task NotifyParametersChangedAsync(this IComponent component)
        {
            await (component.GetType().GetMethod("OnParametersSetAsync", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(component, Array.Empty<object>()) as Task ?? Task.CompletedTask);
        }
    }
}
