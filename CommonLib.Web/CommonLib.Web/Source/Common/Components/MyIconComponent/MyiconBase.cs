using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CommonLib.Web.Source.Models;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.UtilClasses;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using NLog;
using EnumConverter = CommonLib.Source.Common.Converters.EnumConverter;
using Logger = CommonLib.Source.Common.Utils.UtilClasses.Logger;
using StringConverter = CommonLib.Source.Common.Converters.StringConverter;
using CommonLib.Web.Source.Services.Upload.Interfaces;
using Color = SixLabors.ImageSharp.Color;
using IconTypeT = CommonLib.Source.Common.Utils.UtilClasses.IconType;

namespace CommonLib.Web.Source.Common.Components.MyIconComponent
{
    public class MyIconBase : MyComponentBase
    {
        private static OrderedSemaphore _syncGettingIconFromFile { get; set; } = new(1, 1);
        private static string _rootDir;
        private static ConcurrentDictionary<IconType, HtmlNode> _inMemoryIconsCache;
        private static ExtendedTime _lastLocalStorageCaching;

        public static string RootDir => _rootDir ??= FileUtils.GetEntryAssemblyDir(); // ((object) WebUtils.ServerHostEnvironment).GetProperty<string>("ContentRootPath");
        public HtmlNode Svg { get; set; }
     
        [Parameter] 
        public BlazorParameter<IconTypeT> IconType { get; set; }

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
            _inMemoryIconsCache ??= new ConcurrentDictionary<IconType, HtmlNode>();
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

            if (IconType.HasChanged())
            {
                //if (!MyJsRuntime.IsInitialized)
                //    _iconTypeHasChanged = true;
                //else
                    await SetSvgIconAsync();
            }
            
            if (Svg is not null)
            {
                if (InteractivityState.HasChanged() || Color.HasChanged() || SecondaryColor.HasChanged() || IconType.HasChanged() || !IsIconColorSet()) // There are edge cases when IconType changes thus reloading unstyled SVG from cache even when neither InteractivityState nor Colors changed, in such case styling the icon again is necessary
                {
                    SeticonColors();
                }

                if (SizeMode.HasChanged()) // && IconType.HasValue() && _svgXmlns != null && _svgViewBox != null && _dPath != null)
                {
                    SetIconSize();
                }
            }
            
            await Task.CompletedTask;
        }

        //protected override async Task OnAfterRenderAsync(bool firstRender, bool authUserChanged)
        //{
        //    if (firstRender || _iconTypeHasChanged)
        //    {
        //        _iconTypeHasChanged = false;
        //        await SetSvgIconAsync();
        //        SeticonColors();
        //        SetIconSize();
        //        await StateHasChangedAsync(true);
        //    }
        //}

        protected async Task Icon_ClickAsync(MouseEventArgs e)
        {
            var msg = $"{IconType.V} Clicked";
            Logger.For<MyIconBase>().Log(LogLevel.Info, msg);
            await Click.InvokeAsync(this, e).ConfigureAwait(false);
        }

        private async Task SetSvgIconAsync()
        {
            try
            {
                var svg = _inMemoryIconsCache.VorN(IconType.V);
                if (svg is null)
                {
                    svg = await MyJsRuntime.IsInitializedAsync() ? await GetIconFromLocalStorageIconsCacheAsync(IconType.V) : null;
                    if (svg is null)
                    {
                        //svg = _inMemoryIconsCache.VorN(IconType.V) ?? await GetIconFromLocalStorageIconsCacheAsync(IconType.V);
                        svg ??= await GetIconFromFile(IconType.V);

                        async Task<bool> shouldCacheAsync() => await MyJsRuntime.IsInitializedAsync() && (_lastLocalStorageCaching is null || ExtendedTime.UtcNow - _lastLocalStorageCaching > TimeSpan.FromSeconds(10));
                        if (await shouldCacheAsync())
                        {
                            await _syncGettingIconFromFile.WaitAsync();
                            if (await shouldCacheAsync())
                            {
                                await SaveIconsToLocalStorageIconsCacheAsync(_inMemoryIconsCache.Concat(new KeyValuePair<IconTypeT, HtmlNode>(IconType.V, svg)).DistinctBy(kvp => kvp.Key).ToDictionary());
                                _lastLocalStorageCaching = ExtendedTime.UtcNow;
                            }
                            await _syncGettingIconFromFile.ReleaseSafelyAsync();
                        }
                    }
                    
                    _inMemoryIconsCache.TryAdd(IconType.V, svg);
                }
                
                Svg = svg?.CloneNode(true);

                //
            }
            catch (TaskCanceledException)
            {
                Logger.For<MyIconBase>().Warn($"Getting Icon [{IconType.ParameterValue}] was canceled, did you refresh the page or validation state message don't need that icon anymore?");
                await _syncGettingIconFromFile.ReleaseSafelyAsync();
            }
            catch (Exception ex)
            {
                await _syncGettingIconFromFile.ReleaseSafelyAsync();
            }
        }
        
        private void SeticonColors()
        {
            var isEnabled = InteractivityState.V?.IsEnabledOrForceEnabled == true;
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

        private void SetIconSize()
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

        private async Task<ConcurrentDictionary<IconTypeT, HtmlNode>> GetLocalStorageIconsCacheAsync()
        {
            var jIconsCache = (await LocalStorage.GetItemAsStringAsync("IconsCache"))?.JsonDeserialize();
            if (jIconsCache is null)
                return new ConcurrentDictionary<IconType, HtmlNode>();
            var rawDict = jIconsCache.To<Dictionary<string, Dictionary<string, object>>>();
            var finalDict = rawDict.SelectMany(kvp => kvp.Value.Select(kvp2 => new KeyValuePair<IconTypeT, HtmlNode>(IconTypeT.From(kvp.Key, kvp2.Key), kvp2.Value?.ToString().IsHTML() == true ? kvp2.Value.ToString().TrimMultiline().ToHtmlAgility().SelectSingleNode("./svg") : null))).ToConcurrentDictionary();
            return finalDict;
        }

        private async Task<ConcurrentDictionary<IconTypeT, HtmlNode>> SaveLocalStorageIconsCacheAsync(ConcurrentDictionary<IconTypeT, HtmlNode> iconsCache)
        {
            var jstrIconsCache = iconsCache.RemoveNulls().GroupBy(i => i.Key.SetName).ToDictionary(g => g.Key.ToLowerInvariant(), g => g.ToDictionary(kvp => kvp.Key.IconName.PascalCaseToKebabCase(), kvp => kvp.Value?.OuterHtml?.TrimMultiline())).JsonSerialize();
            await LocalStorage.SetItemAsStringAsync("IconsCache", jstrIconsCache);
            return iconsCache;
        }

        private async Task<HtmlNode> GetIconFromLocalStorageIconsCacheAsync(IconTypeT icon) => (await GetLocalStorageIconsCacheAsync()).VorN(icon);
        private async Task<ConcurrentDictionary<IconTypeT, HtmlNode>> SaveIconToLocalStorageIconsCacheAsync(IconTypeT icon, HtmlNode svg, ConcurrentDictionary<IconTypeT, HtmlNode> iconsCache = null)
        {
            var localStorageIconsCache = iconsCache ?? await GetLocalStorageIconsCacheAsync();
            localStorageIconsCache[icon] = svg;
            await SaveLocalStorageIconsCacheAsync(localStorageIconsCache);
            return localStorageIconsCache;
        }

        private async Task<ConcurrentDictionary<IconTypeT, HtmlNode>> SaveIconsToLocalStorageIconsCacheAsync(IDictionary<IconTypeT, HtmlNode> iconsToSave, ConcurrentDictionary<IconTypeT, HtmlNode> iconsCache = null)
        {
            var localStorageIconsCache = iconsCache ?? await GetLocalStorageIconsCacheAsync();
            localStorageIconsCache.AddRange(iconsToSave);
            await SaveLocalStorageIconsCacheAsync(localStorageIconsCache);
            return localStorageIconsCache;
        }

        private async Task<HtmlNode> GetIconFromFile(IconTypeT icon)
        {
            var iconEnums = icon.GetType().GetProperties().Where(p => p.Name.EndsWithInvariant("Icon")).ToArray();
            var iconEnumVals = iconEnums.Select(p => p.GetValue(icon)).ToArray();
            var iconEnum = iconEnumVals.Single(v => v is not null);
            var iconType = iconEnum.GetType();
            var iconName = StringConverter.PascalCaseToKebabCase(EnumConverter.EnumToString(iconEnum.CastToReflected(iconType)));
            var iconSetDirName = iconType.Name.BeforeFirst("IconType");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("browser"))) // if WebAssembly
                return (await UploadClient.GetRenderedIconAsync(icon)).Result?.TrimMultiline().ToHtmlAgility().SelectSingleNode("./svg");
            else
            {
                var iconPath = PathUtils.Combine(PathSeparator.BSlash, RootDir, $@"_myContent\CommonLib.Web\Content\Icons\{iconSetDirName}\{iconName}.svg");
                return (await File.ReadAllTextAsync(iconPath).ConfigureAwait(false)).TrimMultiline().ToHtmlAgility().SelectSingleNode("./svg");
            }
        }

        private bool IsIconColorSet() => Svg?.SelectNodes("./path")?.FirstOrNull()?.GetAttributeValue("style").CssStringToDictionary().VorN("fill").IsNullOrWhiteSpace() != true;
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
