using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Services.Upload;
using CommonLib.Web.Source.Services.Upload.Interfaces;
using HtmlAgilityPack;

namespace CommonLib.Web.Source.Common.Utils.UtilClasses
{
    public class MySvgIcon
    {
        private static string _rootDir;
        public static string RootDir => _rootDir ??= FileUtils.GetEntryAssemblyDir();

        public string Name { get; set; }
        public string Content { get; set; }
        public string ViewBox { get; set; }

        public MySvgIcon(string name, string content, string viewBox)
        {
            Name = name;
            Content = content;
            ViewBox = viewBox;
        }

        public static async Task<HtmlNode> GetIconNodeFromiconTypeAsync(IconType icon, IUploadClient uploadClient = null)
        {
            var iconEnums = icon.GetType().GetProperties().Where(p => p.Name.EndsWithInvariant("Icon")).ToArray();
            var iconEnumVals = iconEnums.Select(p => p.GetValue(icon)).ToArray();
            var iconEnum = iconEnumVals.Single(v => v is not null);
            var iconType = iconEnum.GetType();
            var iconName = StringConverter.PascalCaseToKebabCase(EnumConverter.EnumToString(iconEnum.CastToReflected(iconType)));
            var iconSetDirName = iconType.Name.BeforeFirst("IconType");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("browser")) && uploadClient is not null) // if WebAssembly
                return (await uploadClient.GetRenderedIconAsync(icon)).Result?.TrimMultiline().ToHtmlAgility().SelectSingleNode("./svg");

            var iconPath = PathUtils.Combine(PathSeparator.BSlash, RootDir, $@"_myContent\CommonLib.Web\Content\Icons\{iconSetDirName}\{iconName}.svg");
            return (await File.ReadAllTextAsync(iconPath).ConfigureAwait(false)).TrimMultiline().ToHtmlAgility().SelectSingleNode("./svg");
        }

        public static async Task<MySvgIcon> FromIconTypeAsync(IconType icon, IUploadClient uploadClient = null)
        {
            var svg = await GetIconNodeFromiconTypeAsync(icon, uploadClient);
            var viewBox = svg.GetAttributeValue("viewBox", "");
            var path = svg.SelectSingleNode("./path")?.OuterHtml;
            return new MySvgIcon(icon.ToString(), path, viewBox);
        }
    }
}
