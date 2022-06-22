using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Utils;
using CommonLib.Web.Server.Source.Common.Utils;
using CommonLib.Web.Source.Common.CustomMVCRenderers;

namespace CommonLib.Web.Server.Source.Common.RazorRenderers
{
    public static class CssRendererSave
    {
        private static readonly SemaphoreSlim _syncSaveCssFile = new SemaphoreSlim(1, 1);

        public static async Task<CssRendererReferenceConfig> SaveAs(this Task<CssRendererSaveConfig> config, string path)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var saveConfig = await config.ConfigureAwait(false);
            var physicalPath = PathUtils.Combine(PathSeparator.BSlash, WebServerUtils.HostEnvironment.ContentRootPath, path);

            await _syncSaveCssFile.WaitAsync().ConfigureAwait(false);

            new FileInfo(physicalPath).Directory?.Create();
            await File.WriteAllTextAsync(physicalPath, saveConfig.Styles.ExCssToString());
            var referenceConfig = new CssRendererReferenceConfig(path);

            _syncSaveCssFile.Release();

            return referenceConfig;
        }
    }
}

// @await Html.PartialAsync("_Styles").ExtractCss().SaveAs("~/wwwroot/Styles/Styles.css").Reference().Render()
