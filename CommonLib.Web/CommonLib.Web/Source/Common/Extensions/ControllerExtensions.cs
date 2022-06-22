using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using RedirectResult = Microsoft.AspNetCore.Mvc.RedirectResult;

namespace CommonLib.Web.Source.Common.Extensions
{
    public static class ControllerExtensions
    {
        public static RedirectResult RedirectRelative(this Controller controller, string relativeLocation)
        {
            if (controller == null)
                throw new ArgumentNullException(nameof(controller));

            var miRedirect = controller.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(mi => mi.Name == "Redirect" && mi.GetParameters().Length == 1)
                .Single(mi => mi.GetParameters().Single().ParameterType == typeof(string));

            return (RedirectResult)miRedirect?.Invoke(controller, new object[] { $"{controller.Request.Root()}{relativeLocation}" });
        }
    }
}
