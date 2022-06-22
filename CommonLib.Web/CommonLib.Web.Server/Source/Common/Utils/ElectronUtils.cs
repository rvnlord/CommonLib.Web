using CommonLib.Source.Common.Extensions.Collections;
using ElectronNET.API;

namespace CommonLib.Web.Server.Source.Common.Utils
{
    public static class ElectronUtils
    {
        public static async Task<BrowserWindow> GetWindowOrNullAsync() => await Electron.WindowManager.BrowserWindows.SingleOrDefaultAsync().ConfigureAwait(false);
    }
}
