using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Utils;
using CommonLib.Web.Source.Models;
using Microsoft.AspNetCore.Components;
using SixLabors.ImageSharp;

namespace CommonLib.Web.Source.Common.Components.MyImageComponent
{
    public class MyImageBase : MyComponentBase
    {
        private static string _commonWwwRootDir;
        private static string _currentWwwRootDir;
        private static bool? _isProduction;
        private static ConcurrentDictionary<string, ImgPaths> _imgPathsCache { get; set; }
        private static readonly SemaphoreSlim _syncImageLoad = new(1, 1);
        
        protected string _originalHeight { get; set; }
        protected string _originalWidth { get; set; }
        protected string _expectedWidth { get; set; }
        
        public static string CommonWwwRootDir => _commonWwwRootDir ??= FileUtils.GetAspNetWwwRootDir<MyImageBase>();
        public static string CurrentWwwRootDir => _currentWwwRootDir ??= ((object) WebUtils.ServerHostEnvironment).GetProperty<string>("WebRootPath");
        public static bool IsProduction => _isProduction ??= Directory.Exists(PathUtils.Combine(PathSeparator.BSlash, CurrentWwwRootDir, "_content"));
        
        [Parameter]
        public BlazorParameter<string> Path { get; set; }

        protected override async Task OnInitializedAsync()
        {
            _imgPathsCache ??= new ConcurrentDictionary<string, ImgPaths>();
            await Task.CompletedTask;
        }

        protected override async Task OnParametersSetAsync()
        {
            if (FirstParamSetup)
            {
                SetMainAndUserDefinedClasses("my-image");

                var userSuppliedStyles = GetUserDefinedStyles();
                var customStyles = new Dictionary<string, string>();
                
                if (userSuppliedStyles.VorN("height") == null && userSuppliedStyles.VorN("padding-top") == null)
                    customStyles["padding-top"] = "100%";
                else if (userSuppliedStyles.VorN("height") != null)
                    customStyles["height"] = userSuppliedStyles.VorN("height");
                else if (userSuppliedStyles.VorN("padding-top") != null)
                    customStyles["padding-top"] = userSuppliedStyles.VorN("padding-top");

                userSuppliedStyles.RemoveIfExists("height");
                userSuppliedStyles.RemoveIfExists("padding-top");

                SetCustomStyles(new [] { customStyles, userSuppliedStyles });
                SetUserDefinedAttributes();
            }

            if (Path.HasChanged() && !IsDisposed)
            {
                try
                {
                    await _syncImageLoad.WaitAsync(); // to prevent loading to cache the same image multiple times
                    
                    Image imgData;
                    var imgName = Path.ParameterValue.AfterLastOrWhole("/");
                    
                    var imgPath = _imgPathsCache.VorN(imgName);
                    if (imgPath is null)
                    {
                        var commonAbsoluteVirtualPath = PathUtils.Combine(PathSeparator.FSlash, NavigationManager.BaseUri, @"_content\CommonLib.Web", Path.ParameterValue);
                        var localAbsoluteVirtualPath = PathUtils.Combine(PathSeparator.FSlash, NavigationManager.BaseUri, Path.ParameterValue);

                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("browser"))) // if WebAssembly
                        {
                            var isCommonResource = (await HttpClient.GetAsync(commonAbsoluteVirtualPath)).IsSuccessStatusCode;
                            var bytesImage = isCommonResource ? await HttpClient.GetByteArrayAsync(commonAbsoluteVirtualPath) : await HttpClient.GetByteArrayAsync(localAbsoluteVirtualPath);
                            imgPath = new ImgPaths { Virtual = isCommonResource ? commonAbsoluteVirtualPath : localAbsoluteVirtualPath };
                            imgData = Image.Load(bytesImage);
                        }
                        else
                        {
                            var productionLocalAbsolutePhysicalPath = PathUtils.Combine(PathSeparator.BSlash, CurrentWwwRootDir, $@"{Path.ParameterValue}");
                            var productionCommonAbsolutePhysicalPath = PathUtils.Combine(PathSeparator.BSlash, CurrentWwwRootDir, @"_content\CommonLib.Web", $@"{Path.ParameterValue}");
                            var devLocalAbsolutePhysicalPath = productionLocalAbsolutePhysicalPath;
                            var devCommonAbsolutePhysicalPath = PathUtils.Combine(PathSeparator.BSlash, CommonWwwRootDir, $@"{Path.ParameterValue}");

                            if (!IsProduction)
                            {
                                if (File.Exists(devCommonAbsolutePhysicalPath))
                                    imgPath = new ImgPaths { Physical = devCommonAbsolutePhysicalPath, Virtual = commonAbsoluteVirtualPath };
                                else
                                    imgPath = new ImgPaths { Physical = devLocalAbsolutePhysicalPath, Virtual = localAbsoluteVirtualPath };
                            }
                            else
                            {
                                if (File.Exists(productionCommonAbsolutePhysicalPath))
                                    imgPath = new ImgPaths { Physical = productionCommonAbsolutePhysicalPath, Virtual = commonAbsoluteVirtualPath };
                                else
                                    imgPath = new ImgPaths { Physical = productionLocalAbsolutePhysicalPath, Virtual = localAbsoluteVirtualPath };
                            }
                            
                            imgData = await Image.LoadAsync(imgPath.Physical);
                        }

                        _imgPathsCache[imgName] = imgPath;
                    }
                    else
                        imgData = await Image.LoadAsync(imgPath.Physical);
                    
                    AddOrUpdateStyle("background-image", $"url('{imgPath.Virtual}')");
                    
                    _originalHeight = $"{imgData.Height}px";
                    _originalWidth = $"{imgData.Width}px";
                    _expectedWidth = AdditionalAttributes.VorN("expected-width")?.ToString();

                    _syncImageLoad.Release();
                }
                catch (TaskCanceledException)
                {
                    Logger.For<MyImageBase>().Warn("Getting 'imgData' data has been canceled, did you suddenly refresh the page?");
                }
            }
        }
        
        protected override async Task OnAfterFirstRenderAsync() => await Task.CompletedTask;

        private class ImgPaths
        {
            public string Virtual { get; init; }
            public string Physical { get; init; }
        }
    }

}
