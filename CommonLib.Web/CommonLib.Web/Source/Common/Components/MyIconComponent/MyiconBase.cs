using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Utils;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Models;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.TypeUtils;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using NLog;
using Logger = CommonLib.Source.Common.Utils.UtilClasses.Logger;

namespace CommonLib.Web.Source.Common.Components.MyIconComponent
{
    public class MyIconBase : MyComponentBase
    {
        private static Dictionary<IconType, HtmlNode> _svgCache { get; set; }

        private Dictionary<string, string> _svgStyle { get; set; }
       
        protected bool _disabled;
        protected string _renderSvgStyle { get; set; }
        protected string _svgXmlns { get; set; }
        protected string _svgViewBox { get; set; }
        protected string _dPath { get; set; }
        
        [Parameter] public BlazorParameter<IconType> IconType { get; set; }
        [Parameter] public string Color { get; set; }
        [Parameter] public BlazorParameter<IconSizeMode> SizeMode { get; set; } = IconSizeMode.InheritFromStyles;
        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
        [CascadingParameter] public BlazorParameter<ButtonState?> CascadingButtonState { get; set; }
        [CascadingParameter] public BlazorParameter<InputState> CascadingInputState { get; set; }
        [CascadingParameter] public BlazorParameter<MyButtonBase> CascadingButton { get; set; }

        protected override async Task OnInitializedAsync()
        {
            _svgCache ??= new Dictionary<IconType, HtmlNode>();
            _svgStyle ??= new Dictionary<string, string>();
            await Task.CompletedTask;
        }

        protected override async Task OnParametersSetAsync()
        {
            if (IsFirstParamSetup())
            {
                // Pre-Render
                _renderStyle = new Dictionary<string, string>
                {
                    ["width"] = StylesConfig.FontSize.Px(),
                    ["height"] = StylesConfig.FontSize.Px(),
                    ["max-width"] = StylesConfig.FontSize.Px(),
                    ["max-height"] = StylesConfig.FontSize.Px(),
                    ["margin"] = (StylesConfig.FontSize / 4).Px(),
                    ["overflow"] = "hidden",
                    ["opacity"] = "0"
                }.CssDictionaryToString();
                _renderClasses = new List<string> { "my-icon", "pre-render" }.JoinAsString(" ");
            }

            if (IconType.ParameterValue == null)
            {
                _svgXmlns = null;
                _svgViewBox = null;
                _dPath = null;
                return;
            }

            if (CascadingButton.HasChanged() && CascadingButton.HasValue())
                CascadingButton.ParameterValue.OtherIcons[IconType.ParameterValue] = this;

            if (CascadingButtonState.HasValue() && CascadingButtonState.ParameterValue == ButtonState.Disabled
                || CascadingInputState.HasValue() && CascadingInputState.ParameterValue == InputState.Disabled)
            {
                _disabled = true;
            }
            else
                _disabled = false;

            if (IconType.HasChanged())
            {
                var iconEnums = IconType.ParameterValue.GetType().GetProperties().Where(p => p.Name.EndsWithInvariant("Icon")).ToArray();
                var iconEnumVals = iconEnums.Select(p => p.GetValue(IconType.ParameterValue)).ToArray();
                var iconEnum = iconEnumVals.Single(v => v != null);

                var iconType = iconEnum.GetType();
                var iconName = StringConverter.PascalCaseToKebabCase(EnumConverter.EnumToString(iconEnum.CastToReflected(iconType)));
                var iconSetDirName = iconType.Name.BeforeFirst("IconType");
                var iconPath = PathUtils.Combine(PathSeparator.FSlash, NavigationManager.BaseUri, $@"Icons/{iconSetDirName}/{iconName}.svg");

                try
                {
                    //if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("browser"))) // if WebAssembly
                    //{
                    var svg = _svgCache.VorN(IconType.ParameterValue);
                    if (svg == null)
                    {
                        svg = (await HttpClient.GetStringAsync(new Uri(iconPath))).TrimMultiline().ToHtmlAgility().SelectSingleNode("./svg");
                        _svgCache[IconType.ParameterValue] = svg;
                    }
                    //}
                    //else
                    //{
                    //    iconPath = $@"{WebUtils.GetAbsolutePhysicalContentPath()}\Content\Icons\{iconSetDirName}\{iconName}.svg";
                    //    svg = (await File.ReadAllTextAsync(iconPath).ConfigureAwait(false)).TrimMultiline().ToHtmlAgility().SelectSingleNode("./svg");
                    //}

                    _svgXmlns = svg.GetAttributeValue("xmlns");
                    _svgViewBox = svg.GetAttributeValue("viewBox");
                    _dPath = svg.SelectSingleNode("./path").GetAttributeValue("d");
                }
                catch (TaskCanceledException)
                {
                    Logger.For<MyIconBase>().Warn($"Getting Icon [{IconType.ParameterValue}] was canceled, did you refresh the page or validation state message don't need that icon anymore?");
                }
            }

            if (SizeMode.HasChanged() && IconType.HasValue() && _svgXmlns != null && _svgViewBox != null && _dPath != null)
            {
                if (!_svgViewBox.IsNullOrWhiteSpace())
                {
                    // enforce quadratic and over
                    var vbWidth = _svgViewBox.Split(" ")[2].ToInt();
                    var vbHeight = _svgViewBox.Split(" ")[3].ToInt();
                    var sizeMode = SizeMode?.ParameterValue;

                    if (sizeMode == IconSizeMode.Contain && vbWidth >= vbHeight
                        || sizeMode is IconSizeMode.Cover or IconSizeMode.EnforceQuadraticAndCover && vbWidth < vbHeight
                        || sizeMode == IconSizeMode.FillWidth)
                        _svgStyle.ReplaceAll(new Dictionary<string, string> { ["width"] = "100%", ["height"] = "auto" });
                    else if (sizeMode == IconSizeMode.Contain && vbWidth < vbHeight
                             || sizeMode == IconSizeMode.Cover && vbWidth >= vbHeight
                             || sizeMode == IconSizeMode.FillHeight)
                        _svgStyle.ReplaceAll(new Dictionary<string, string> { ["width"] = "auto", ["height"] = "100%" });
                    else if (sizeMode == IconSizeMode.InheritFromStyles)
                        _svgStyle.Clear();
                    _renderSvgStyle = _svgStyle.CssDictionaryToString();
                }
            }

            if (IsFirstParamSetup() || _renderClasses.Split(" ").Contains("pre-render")) // 2nd condition for when the component was disposed before the styles were set
            {
                SetMainCustomAndUserDefinedClasses("my-icon", SizeMode?.ParameterValue == IconSizeMode.EnforceQuadraticAndCover ? new[] { "my-quadratic" } : null);
                SetUserDefinedStyles();
                SetUserDefinedAttributes();
            }
        }

        protected override async Task OnAfterFirstRenderAsync() => await Task.CompletedTask;

        protected async Task PrintAndClickCallbackAsync(MouseEventArgs e)
        {
            var msg = $"{IconType} Clicked";
            Logger.For<MyIconBase>().Log(LogLevel.Info, msg);

            await OnClick.InvokeAsync(e).ConfigureAwait(false);
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
