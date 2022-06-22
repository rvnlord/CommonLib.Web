using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace CommonLib.Web.Source.Common.Extensions
{
    public static class ComponentBaseExtensions
    {
        public static async Task OnParametersSetAsync<TComponent>(this TComponent component) where TComponent : IComponent
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            var onParamsSet = component.GetType().GetMethod("OnParametersSetAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            if (onParamsSet?.Invoke(component, Array.Empty<object>()) is Task onParamsSetResult)
                await onParamsSetResult.ConfigureAwait(false);
            else
                throw new MissingMethodException("OnParametersSetAsync");
        }

        public static void BuildRenderTree<TComponent>(this TComponent component, RenderTreeBuilder builder) where TComponent : IComponent
        {
            var buildRenderTree = component?.GetType().GetMethod("BuildRenderTree", BindingFlags.NonPublic | BindingFlags.Instance);
            buildRenderTree?.Invoke(component, new object[] { builder });
        }
    }
}
