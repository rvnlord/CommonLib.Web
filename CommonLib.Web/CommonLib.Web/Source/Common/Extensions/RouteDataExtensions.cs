using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Routing;

namespace CommonLib.Web.Source.Common.Extensions
{
    public static class RouteDataExtensions
    {
        public static string GetRequiredString(this Microsoft.AspNetCore.Routing.RouteData routeData, string keyName)
        {
            if (routeData == null)
                throw new ArgumentNullException(nameof(routeData));

            if (!routeData.Values.TryGetValue(keyName, out var value))
                throw new InvalidOperationException($"Could not find key with name '{keyName}'");

            return value?.ToString();
        }

        public static string GetRequiredString(this Microsoft.AspNetCore.Components.RouteData routeData, string keyName)
        {
            if (routeData == null)
                throw new ArgumentNullException(nameof(routeData));

            if (!routeData.RouteValues.TryGetValue(keyName, out var value))
                throw new InvalidOperationException($"Could not find key with name '{keyName}'");

            return value?.ToString();
        }
    }
}
