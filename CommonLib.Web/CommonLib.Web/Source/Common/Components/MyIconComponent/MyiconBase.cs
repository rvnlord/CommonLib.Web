using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Models;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Utils;
using CommonLib.Web.Source.Services;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using NLog;
using EnumConverter = CommonLib.Source.Common.Converters.EnumConverter;
using Logger = CommonLib.Source.Common.Utils.UtilClasses.Logger;
using StringConverter = CommonLib.Source.Common.Converters.StringConverter;
using CommonLib.Web.Source.Common.Components.MyDropDownComponent;
using CommonLib.Web.Source.Common.Components.MyInputGroupComponent;
using CommonLib.Web.Source.Common.Components.MyTextInputComponent;
using CommonLib.Web.Source.Common.Converters;
using CommonLib.Web.Source.Services.Upload.Interfaces;
using Color = SixLabors.ImageSharp.Color;

namespace CommonLib.Web.Source.Common.Components.MyIconComponent
{
    public class MyIconBase : MyComponentBase
    {
        //private static string _commonWwwRootDir;
        private static string _rootDir;
        //private static bool? _isProduction;
       
        //public static string CommonWwwRootDir => _commonWwwRootDir ??= FileUtils.GetAspNetWwwRootDir<MyIconBase>();
        public static string RootDir => _rootDir ??= FileUtils.GetEntryAssemblyDir(); // ((object) WebUtils.ServerHostEnvironment).GetProperty<string>("ContentRootPath");
        //public static bool IsProduction => _isProduction ??= Directory.Exists(PathUtils.Combine(PathSeparator.BSlash, CurrentWwwRootDir, "_content"));
        public HtmlNode Svg { get; set; }
        public static ConcurrentDictionary<IconType, HtmlNode> SvgCache { get; set; }
        public RenderFragment ComplexSvg { get; set; }

        [Parameter] 
        public BlazorParameter<IconType> IconType { get; set; }

        [Parameter] 
        public BlazorParameter<Color?> Color { get; set; }

        [Parameter] 
        public BlazorParameter<Color?> SecondaryColor { get; set; }

        [Parameter] 
        public BlazorParameter<IconSizeMode?> SizeMode { get; set; }

        [Parameter] 
        public MyAsyncEventHandler<MyIconBase, MouseEventArgs> Click { get; set; }

        [Inject] 
        public IUploadClient UploadClient { get; set; }

        protected override async Task OnInitializedAsync()
        {
            SvgCache ??= new ConcurrentDictionary<IconType, HtmlNode>();
            await Task.CompletedTask;
        }

        protected override async Task OnParametersSetAsync()
        {
            if (FirstParamSetup)
            {
                var customClasses = new List<string> { "my-icon" }; // "pre-render"
                SetMainCustomAndUserDefinedClasses("my-icon", customClasses);
                SetUserDefinedStyles();
                SetUserDefinedAttributes();
            }
            
            var iconEnums = IconType.ParameterValue.GetType().GetProperties().Where(p => p.Name.EndsWithInvariant("Icon")).ToArray();
            var iconEnumVals = iconEnums.Select(p => p.GetValue(IconType.ParameterValue)).ToArray();
            var iconEnum = iconEnumVals.Single(v => v != null);
            var iconType = iconEnum.GetType();
            var iconName = StringConverter.PascalCaseToKebabCase(EnumConverter.EnumToString(iconEnum.CastToReflected(iconType)));

            if (IconType.HasChanged())
            {
                var iconSetDirName = iconType.Name.BeforeFirst("IconType");

                try
                {
                    var svg = SvgCache.VorN(IconType.ParameterValue);
                    if (svg is null)
                    {
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("browser"))) // if WebAssembly
                            svg = (await UploadClient.GetRenderedIconAsync(IconType.V)).Result?.TrimMultiline().ToHtmlAgility().SelectSingleNode("./svg");
                        else
                        {
                            var iconPath = PathUtils.Combine(PathSeparator.BSlash, RootDir, $@"_myContent\CommonLib.Web\Content\Icons\{iconSetDirName}\{iconName}.svg");
                            svg = (await File.ReadAllTextAsync(iconPath).ConfigureAwait(false)).TrimMultiline().ToHtmlAgility().SelectSingleNode("./svg");
                        }

                        SvgCache.TryAdd(IconType.ParameterValue, svg);
                    }

                    Svg = svg?.CloneNode(true);
                }
                catch (TaskCanceledException)
                {
                    Logger.For<MyIconBase>().Warn($"Getting Icon [{IconType.ParameterValue}] was canceled, did you refresh the page or validation state message don't need that icon anymore?");
                }
            }

            //if (Color.HasChanged() && !InteractionState.HasChanged() && InteractionState.V?.IsEnabledButNotForced == true)
            //{
            //    if (Svg is null)
            //        throw new NullReferenceException("Svg Icon is null");

            //    if (Color.HasValue())
            //        SetPrimaryColorPath(Color.V);
            //}

            //if (SecondaryColor.HasChanged() && !InteractionState.HasChanged() && InteractionState.V?.IsEnabledButNotForced == true)
            //{
            //    if (Svg is null)
            //        throw new NullReferenceException("Svg Icon is null");

            //    if (SecondaryColor.HasValue())
            //        SetSecondaryColorPath(SecondaryColor.V);
            //}

            if (IconType.V == CommonLib.Source.Common.Utils.UtilClasses.IconType.From(LightIconType.Key))
            {
                var t = 0;
            }

            if (InteractionState.HasChanged() || Color.HasChanged() || SecondaryColor.HasChanged() || IconType.HasChanged()) // There are edge cases when IconType changes thus reloading unstyled SVG from cache even when neither InteractionState nor Colors changed, in such case styling the icon again is necessary
            {
                var isEnabled = InteractionState.V?.IsEnabledOrForceEnabled == true;
                var pathsCount = Svg?.SelectNodes("./path")?.Count ?? 0;

                if (isEnabled)
                {
                    if (pathsCount.In(1, 2))
                    {
                        SetPrimaryColorPath(Color.V);
                        SetSecondaryColorPath(SecondaryColor.V);
                    }
                    else
                    {
                        AddStyle("filter", "none");
                    }
                }
                else
                {
                    if (pathsCount.In(1, 2))
                    {
                        var disabledColor = "#404040".HexToColor();
                        SetPrimaryColorPath(disabledColor);
                        SetSecondaryColorPath(disabledColor);
                    }
                    else
                    {
                        AddStyle("filter", "grayscale(1) brightness(0.2)");
                    }
                }
            }

            if (SizeMode.HasChanged()) // && IconType.HasValue() && _svgXmlns != null && _svgViewBox != null && _dPath != null)
            {
                if (Svg is null)
                    throw new NullReferenceException("Svg Icon is null");

                SizeMode.ParameterValue ??= IconSizeMode.InheritFromStyles;

                if (SizeMode?.ParameterValue == IconSizeMode.EnforceQuadraticAndCover)
                    AddClass("my-quadratic");
                
                var svgViewBox = Svg.GetAttributeValue("viewBox");
                var vbWidth = svgViewBox.Split(" ")[2].ToInt();
                var vbHeight = svgViewBox.Split(" ")[3].ToInt();
                var sizeMode = SizeMode?.ParameterValue;
                var sizeSvgStyle = new Dictionary<string, string>();

                switch (sizeMode)
                {
                    case IconSizeMode.Contain when vbWidth >= vbHeight:
                    case IconSizeMode.Cover or IconSizeMode.EnforceQuadraticAndCover when vbWidth < vbHeight:
                    case IconSizeMode.FillWidth:
                        sizeSvgStyle.ReplaceAll(new Dictionary<string, string> { ["width"] = "100%", ["height"] = "auto" });
                        break;
                    case IconSizeMode.Contain when vbWidth < vbHeight:
                    case IconSizeMode.Cover when vbWidth >= vbHeight:
                    case IconSizeMode.FillHeight:
                        sizeSvgStyle.ReplaceAll(new Dictionary<string, string> { ["width"] = "auto", ["height"] = "100%" });
                        break;
                    case IconSizeMode.InheritFromStyles:
                        sizeSvgStyle.Clear();
                        break;
                }

                var svgStyle = Svg.GetAttributeValue("style").CssStringToDictionary();
                svgStyle.AddRange(sizeSvgStyle);
                Svg.SetAttributeValue("style", svgStyle.CssDictionaryToString());
            }
        }
        
        protected async Task Icon_ClickAsync(MouseEventArgs e)
        {
            var msg = $"{IconType.V} Clicked";
            Logger.For<MyIconBase>().Log(LogLevel.Info, msg);
            await Click.InvokeAsync(this, e).ConfigureAwait(false);
        }

        private void SetPrimaryColorPath(Color? color)
        {
            var paths = Svg.SelectNodes("./path");
            var primaryPath = paths?.Count switch
            {
                1 => paths.SingleOrNull(),
                2 => paths.SingleOrNull(p => p.HasClass("fa-primary")),
                _ => null
            };

            if (primaryPath is not null)
            {
                var primaryPathStyle = primaryPath.GetAttributeValue("style").CssStringToDictionary();
                primaryPathStyle["fill"] = color?.ToHex().ToLower().Prepend("#");
                primaryPath.SetAttributeValue("style", primaryPathStyle.CssDictionaryToString());
            }
        }

        private void SetSecondaryColorPath(Color? color)
        {
            var paths = Svg.SelectNodes("./path");
            var secondaryPath = paths?.Count switch
            {
                2 => paths.SingleOrNull(p => p.HasClass("fa-secondary")),
                _ => null
            };

            if (secondaryPath is not null)
            {
                var secondaryPathStyle = secondaryPath.GetAttributeValue("style").CssStringToDictionary();
                secondaryPathStyle["fill"] = color?.ToHex().ToLower().Prepend("#");
                secondaryPath.SetAttributeValue("style", secondaryPathStyle.CssDictionaryToString());
            }
        }
    }

    public enum IconSizeMode
    {
        Contain,
        Cover,
        FillHeight,
        FillWidth,
        EnforceQuadraticAndCover,
        InheritFromStyles
    }
}
