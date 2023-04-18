using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Utils;
using CommonLib.Web.Source.Models;
using CommonLib.Web.Source.Services.Upload.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.FileSystemGlobbing;
using SixLabors.ImageSharp;

namespace CommonLib.Web.Source.Common.Components.MyImageComponent
{
    public class MyImageBase : MyComponentBase
    {
        private static string _rootDir;
        private static string _wwwRootDir;
        private static string _commonWwwRootDir;
        private static bool? _isProduction;
        private static ConcurrentDictionary<string, FileData> _imagesCache { get; set; }
        private readonly SemaphoreSlim _syncImageLoad = new(1, 1);

        protected string _originalHeight { get; set; }
        protected string _originalWidth { get; set; }
        protected string _expectedWidth { get; set; }

        public static string WwwRootDir => _wwwRootDir ??= ((object) WebUtils.ServerHostEnvironment).GetProperty<string>("WebRootPath");
        public static string RootDir => _rootDir ??= FileUtils.GetEntryAssemblyDir();
        public static bool IsWebAssembly => RuntimeInformation.IsOSPlatform(OSPlatform.Create("browser"));
        public static string CommonWwwRootDir => _commonWwwRootDir ??= FileUtils.GetAspNetWwwRootDir<MyImageBase>();
        public static bool IsProduction => _isProduction ??= Directory.Exists(PathUtils.Combine(PathSeparator.BSlash, WwwRootDir, "_content"));
        
        [Parameter]
        public BlazorParameter<object> Path { get; set; } // string path, IEnumerable<byte> data, string base64image OR FileData imageFIle

        [Inject] 
        public IUploadClient UploadClient { get; set; }

        protected override async Task OnInitializedAsync()
        {
            _imagesCache ??= new ConcurrentDictionary<string, FileData>();
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
                customStyles["flex"] = "1 0 auto";

                userSuppliedStyles.RemoveIfExists("height");
                userSuppliedStyles.RemoveIfExists("padding-top");

                SetCustomStyles(new[] { customStyles, userSuppliedStyles });
                SetUserDefinedAttributes();
            }

            if (Path.HasChanged() && !IsDisposed)
            {
                if (Path.V is null || Path.V is string strpath && strpath.IsNullOrWhiteSpace())
                    throw new NullReferenceException("Path to the image can't be empty");

                if (Path.V is string) 
                    Path.ParameterValue = Path.V.ToString()?.TrimStart('\\', '/', '~');

                try
                {
                    await _syncImageLoad.WaitAsync(); // to prevent loading to cache the same image multiple times

                    var imgIdentifier = GetImageIdentifier();
                    var imgData = _imagesCache.VorN(imgIdentifier);
                 
                    if (imgData is null)
                    {
                        if (Path.V is FileData fd)
                            imgData = fd;
                        else
                        {
                            var path = Path.V.ToString();
                            if (IsWebAssembly)
                            {
                                var commonAbsoluteVirtualPath = PathUtils.Combine(PathSeparator.FSlash, NavigationManager.BaseUri, @"_content\CommonLib.Web");
                                var localAbsoluteVirtualPath = PathUtils.Combine(PathSeparator.FSlash, NavigationManager.BaseUri);
                                var isCommonResource = (await HttpClient.GetAsync(commonAbsoluteVirtualPath)).IsSuccessStatusCode;
                                imgData = (await HttpClient.GetByteArrayAsync(isCommonResource ? commonAbsoluteVirtualPath : localAbsoluteVirtualPath)).ToFileData(path.PathToExtension(), path.PathToName());
                            }
                            else
                            {
                                var productionLocalAbsolutePhysicalDir = PathUtils.Combine(PathSeparator.BSlash, WwwRootDir);
                                var productionCommonAbsolutePhysicalDir = PathUtils.Combine(PathSeparator.BSlash, WwwRootDir, @"_content\CommonLib.Web");
                                var devLocalAbsolutePhysicalDir = productionLocalAbsolutePhysicalDir;
                                var devCommonAbsolutePhysicalDir = PathUtils.Combine(PathSeparator.BSlash, CommonWwwRootDir);
                                var sourceFilesMatcher = new Matcher().AddInclude(path);

                                if (!IsProduction)
                                {
                                    if (Directory.Exists(devCommonAbsolutePhysicalDir))
                                        imgData = sourceFilesMatcher.GetResultsInFullPath(devCommonAbsolutePhysicalDir).SingleOrDefault()?.PathToFileData(true);
                                    if (Directory.Exists(devLocalAbsolutePhysicalDir) && imgData is null)
                                        imgData = sourceFilesMatcher.GetResultsInFullPath(devLocalAbsolutePhysicalDir).SingleOrDefault()?.PathToFileData(true);
                                }
                                else
                                {
                                    if (Directory.Exists(productionCommonAbsolutePhysicalDir))
                                        imgData = sourceFilesMatcher.GetResultsInFullPath(productionCommonAbsolutePhysicalDir).SingleOrDefault()?.PathToFileData(true);
                                    if (Directory.Exists(productionLocalAbsolutePhysicalDir) && imgData is null)
                                        imgData = sourceFilesMatcher.GetResultsInFullPath(productionLocalAbsolutePhysicalDir).SingleOrDefault()?.PathToFileData(true);
                                }
                            }     
                        }

                        _imagesCache[imgIdentifier] = imgData;
                    }

                    AddStyle("background-image", $"url('{imgData.ToBase64ImageString()}')");

                    var image = Image.Load(imgData?.Data?.ToArray());
                    _originalHeight = $"{image.Height}px";
                    _originalWidth = $"{image.Width}px";
                    _expectedWidth = AdditionalAttributes.VorN("expected-width")?.ToString();
                }
                catch (TaskCanceledException)
                {
                    Logger.For<MyImageBase>().Warn("Getting 'imgData' data has been canceled, did you suddenly refresh the page?");
                }
                finally
                {
                    _syncImageLoad.Release();
                }
            }

            if (InteractionState.HasChanged())
            {
                AddStyle("filter", InteractionState.V.IsEnabledOrForceEnabled ? "none" : "grayscale(1) brightness(0.2)");
            }
        }

        protected override async Task OnAfterFirstRenderAsync() => await Task.CompletedTask;

        private string GetImageIdentifier()
        {
            if (Path.V is string strPath)
            {
                if (strPath.IsBase64ImageString())
                    return "b64-" + strPath.After(";base64,").TakeLast(40).Keccak256().HexToBase64().Take(20);
                return "path-" + strPath;
            }

            if (Path.V is FileData fd)
                return "fd-" + fd.NameExtensionAndSize.UTF8ToBase64().Take(40).Keccak256().HexToBase64().Take(20); // I specifically don't want to waste time for calculating hashes | beginning of the base64 string is the same for all files

            throw new FormatException("Image Path should be absolute, virtual or point to FileData");
        }
    }

}
