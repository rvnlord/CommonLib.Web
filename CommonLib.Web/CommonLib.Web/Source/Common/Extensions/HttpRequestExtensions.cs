using System;
using CommonLib.Source.Common.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace CommonLib.Web.Source.Common.Extensions
{
    public static class HttpRequestExtensions
    {
        public static string Root(this HttpRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var httpContext = request.HttpContext;
            var routingFeature = httpContext.Features.Get<IRoutingFeature>();
            var actionContext = new ActionContext(
                httpContext,
                routingFeature.RouteData,
                new ActionDescriptor());

            return new UrlHelper(actionContext).Content("~").SkipLast(1);
        }
    }
}
