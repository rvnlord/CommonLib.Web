using System.Threading;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Source.Common.Utils;

namespace CommonLib.Web.Source
{
    public static class AspNetLibConfig
    {
        public static readonly SemaphoreSlim _syncMainCssFile = new SemaphoreSlim(1, 1);
        public static bool IsElectron { get; set; }
        public static RunType RunType { get; set; }
    }
}
