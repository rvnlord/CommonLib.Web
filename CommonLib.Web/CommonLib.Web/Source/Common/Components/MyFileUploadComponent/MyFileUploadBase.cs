﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Models;
using CommonLib.Web.Source.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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

        [Parameter]
        public BlazorParameter<FileSize?> ChunkSize { get; set; }

        public List<FileData> Files { get => Value; set => Value = value; }

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

            if (ChunkSize.HasChanged())
                ChunkSize.ParameterValue ??= new FileSize(32, FileSizeSuffix.KB);
            
            CascadedEditContext.BindValidationStateChanged(CurrentEditContext_ValidationStateChangedAsync);
            await Task.CompletedTask;
        }

        protected override async Task OnAfterFirstRenderAsync()
        {
            await (await ModuleAsync).InvokeVoidAndCatchCancellationAsync("blazor_FileUpload_AfterFirstRender", _guid, DotNetObjectReference.Create(this));
        }

        [JSInvokable]
        public async Task AddFilesToUploadAsync(List<FileData> filesData)
        {
            Files.AddRange(filesData);
            await StateHasChangedAsync(true);
        }

        protected async Task BtnUpload_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken _)
        {
            if (e.Button != 0)
                return;

            var fileCssClass = sender.Classes.Single(c => c.StartsWith("my-file"));
            var btnsToDisable = sender.Siblings.OfType<MyButtonBase>().Where(b => b != sender && b.Classes.Contains(fileCssClass)).ToArray();
            await SetControlStatesAsync(ComponentStateKind.Disabled, btnsToDisable, sender);

            await UploadFileAsync(CssClassToFileData(fileCssClass));
        }

        protected Task BtnClear_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        protected string FileDataToCssClass(FileData fd) => $"my-file-{$"{fd.Name}|{fd.Extension}|{fd.TotalSize.SizeInBytes}".UTF8ToBase58()}";

        protected FileData CssClassToFileData(string cssClass)
        {
            var (name, extension, strSizeInBytes) = cssClass.AfterFirst("my-file-").Base58ToUTF8().Split("|").ToTupleOf3();
            var sizeInBytes = strSizeInBytes.ToLong();
            return Files.Single(f => f.Name.EqualsInvariant(name) && f.Extension.EqualsInvariant(extension) && f.TotalSize.SizeInBytes == sizeInBytes);
        }

        private async Task UploadFileAsync(FileData fd)
        {
            while (!fd.Status.In(UploadStatus.Paused, UploadStatus.Finished, UploadStatus.Failed))
            {
                var chunk = await (await ModuleAsync).InvokeAndCatchCancellationAsync<List<byte>>("blazor_FileUpload_GetFileChunk", _guid, fd.Name, fd.Extension, fd.TotalSizeInBytes, fd.Position, ChunkSize.V?.SizeInBytes);
                // use chosen server method to upload chunk
                var t = 0;
            }
        }
    }
}