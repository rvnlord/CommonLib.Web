using System;
using Microsoft.AspNetCore.Routing;

namespace CommonLib.Web.Source.Common.Extensions
{
    public static class RouteDataExtensions
    {
        public static string GetRequiredString(this RouteData routeData, string keyName)
        {
            if (routeData == null)
                throw new ArgumentNullException(nameof(routeData));

            if (!routeData.Values.TryGetValue(keyName, out var value))
                throw new InvalidOperationException($"Could not find key with name '{keyName}'");

            return value?.ToString();
        }
    }
}
