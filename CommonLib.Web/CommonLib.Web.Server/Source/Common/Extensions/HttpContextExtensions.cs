using CommonLib.Source.Common.Extensions;
using Microsoft.AspNetCore.Http;

namespace CommonLib.Web.Server.Source.Common.Extensions
{
    public static class HttpContextExtensions
    {
        public static string GetRelativeVirtualPath(this HttpContext hc)
        {
            if (hc == null)
                throw new ArgumentNullException(nameof(hc));

            return hc.Request.PathBase.ToString();
        }

        public static string GetRelativeVirtualPath(this HttpContext hc, string virtualAddress)
        {
            if (hc == null)
                throw new ArgumentNullException(nameof(hc));

            var basePath = hc.Request.PathBase.ToString();
            //if (virtualAddress == null)
            //    return basePath;

            virtualAddress = virtualAddress.Trim();
            while (virtualAddress.StartsWithAny("~", "/"))
                virtualAddress = virtualAddress.Skip(1);

            return basePath + "/" + virtualAddress;
        }
    }
}
