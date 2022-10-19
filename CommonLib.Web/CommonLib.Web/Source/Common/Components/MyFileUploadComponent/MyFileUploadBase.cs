using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils;
using CommonLib.Web.Source.Models;
using CommonLib.Web.Source.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Truncon.Collections;

namespace CommonLib.Web.Source.Common.Components.MyFileUploadComponent
{
    public class MyFileUploadBase : MyInputBase<List<FileData>>
    {
        protected OrderedDictionary<string, string> _thumbnailContainerStyle { get; } = new();
        protected string _thumbnailContainerRenderStyle => _thumbnailContainerStyle.CssDictionaryToString();

        [Parameter]
        public BlazorParameter<Func<ExtendedImage>> PreviewFor { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            if (FirstParamSetup)
            {
                SetMainAndUserDefinedClasses("my-fileupload", true);
                SetUserDefinedStyles();
                SetUserDefinedAttributes();
            }

            Model ??= CascadedEditContext?.ParameterValue?.Model;

            string displayName = null;
            if (For != null && Model != null)
                (_, _propName, Value, displayName) = For.GetModelAndProperty();
           
            Placeholder = !Placeholder.IsNullOrWhiteSpace() ? Placeholder : !displayName.IsNullOrWhiteSpace() ? $"{displayName}..." : null;

            if (PreviewFor.HasChanged())
            {
                var previewImage = PreviewFor.V();
                _thumbnailContainerStyle.AddOrUpdate("background-image", $"url('{previewImage.ToBase64DataUrl()}')");
            }

            if (State.HasChanged())
            {
                State.ParameterValue ??= InputState.Disabled;
                if (State.ParameterValue.IsDisabledOrForceDisabled)
                {
                    AddAttribute("disabled", string.Empty);
                    AddClass("disabled");
                    if (_thumbnailContainerStyle.VorN("background-image") is not null)
                        _thumbnailContainerStyle.AddOrUpdate("opacity", "0.3");
                }
                else
                {
                    RemoveAttribute("disabled");
                    RemoveClass("disabled");
                    _thumbnailContainerStyle.RemoveIfExists("opacity");
                }
            }
            
            CascadedEditContext.BindValidationStateChanged(CurrentEditContext_ValidationStateChangedAsync);
            await Task.CompletedTask;
        }

        protected override async Task OnAfterFirstRenderAsync()
        {
            await ModuleAsync;
        }

        [JSInvokable]
        public static async Task AddFilesToUploadAsync(Guid sessionId, Guid guid, List<FileData> filesData)
        {
            var fileUpload = await WebUtils.GetService<ISessionCacheService>()[sessionId].CurrentLayout.ComponentByGuidAsync<MyFileUploadBase>(guid);
            fileUpload.Value.AddRange(filesData);
            await fileUpload.StateHasChangedAsync(true);
        }
    }
}