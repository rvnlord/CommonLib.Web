using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.UtilClasses;
using Microsoft.AspNetCore.Components;

namespace CommonLib.Web.Source.Common.Components.MyImageComponent
{
    public class MyImageBase : MyComponentBase
    {
        protected string _imagePath { get; set; }
        protected string _originalHeight { get; set; }
        protected string _originalWidth { get; set; }
        protected string _expectedWidth { get; set; }
        
        [Parameter]
        public string Path { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            SetMainAndUserDefinedClasses("my-image");

            var userSuppliedStyles = GetUserDefinedStyles();

            var customStyles = new Dictionary<string, string>();
            if (!Path.IsNullOrWhiteSpace())
            {
                _imagePath = PathUtils.Combine(PathSeparator.FSlash, NavigationManager.BaseUri, Path.AfterLastOrWhole("~/"));
                customStyles["background-image"] = $"url('{_imagePath}')";
            }

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

            try
            {
                var imgData = await HttpClient.GetByteArrayAsync(new Uri(PathUtils.Combine(PathSeparator.FSlash, NavigationManager.BaseUri, Path)));
                var origImg = SixLabors.ImageSharp.Image.Load(imgData);
                _originalHeight = $"{origImg.Height}px";
                _originalWidth = $"{origImg.Width}px";
                _expectedWidth = AdditionalAttributes.VorN("expected-width")?.ToString();
            }
            catch (TaskCanceledException)
            {
                Logger.For<MyImageBase>().Warn("Getting 'imgData' data has been canceled, did you suddenly refresh the page?");
            }
        }

        protected override async Task OnInitializedAsync() => await Task.CompletedTask;
        protected override async Task OnAfterFirstRenderAsync() => await Task.CompletedTask;
    }
}
