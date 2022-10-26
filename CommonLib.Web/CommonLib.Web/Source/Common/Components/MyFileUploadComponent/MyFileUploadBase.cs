using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.TypeUtils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Source.Models.Interfaces;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Components.MyPromptComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Models;
using CommonLib.Web.Source.Services.Upload.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;using MoreLinq;
using Truncon.Collections;

namespace CommonLib.Web.Source.Common.Components.MyFileUploadComponent
{
    public class MyFileUploadBase : MyInputBase<List<FileData>>
    {
        protected OrderedDictionary<string, string> _thumbnailContainerStyle { get; } = new();
        protected string _thumbnailContainerRenderStyle => _thumbnailContainerStyle.CssDictionaryToString();

        public IReadOnlyList<FileData> Files => Value.ToList();

        [Parameter]
        public BlazorParameter<Func<ExtendedImage>> PreviewFor { get; set; }

        [Parameter]
        public BlazorParameter<FileSize?> ChunkSize { get; set; }

        [Parameter]
        public BlazorParameter<string> SaveUrl { get; set; }

        [Parameter]
        public BlazorParameter<PredefinedSaveUrlKind?> PredefinedSaveUrl { get; set; }

        [Inject]
        public IUploadClient UploadClient { get; set; }

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
                ChunkSize.ParameterValue ??= new FileSize(2, FileSizeSuffix.MB);

            if (PredefinedSaveUrl.HasChanged() || SaveUrl.HasChanged())
            {
                if (PredefinedSaveUrl.HasValue() && SaveUrl.HasValue() || !PredefinedSaveUrl.HasValue() && !SaveUrl.HasValue())
                    throw new ArgumentException("Upload controller should be either predefined or have a defined upload url but not both");
            }

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
            Value.AddRange(filesData);
            await StateHasChangedAsync(true);
        }

        protected async Task BtnUpload_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken _)
        {
            await CatchAllExceptionsAsync(async () =>
            {
                var btnsToDisable = sender.Siblings.OfType<MyButtonBase>().Where(b => (b.Model?.V as FileData)?.Equals((FileData)sender.Model.V) == true).ToArray();
                await SetControlStatesAsync(ComponentStateKind.Disabled, btnsToDisable, sender);

                await UploadFileAsync((FileData)sender.Model.V);

                await SetControlStatesAsync(ComponentStateKind.Enabled, btnsToDisable);
            });
        }

        protected async Task BtnPause_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken token)
        {
            ((FileData)sender.Model.V).Status = UploadStatus.Paused;
            await SetControlStatesAsync(ComponentStateKind.Loading, new[] { sender });
        }

        protected async Task BtnResume_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken _)
        {
            var btnsToDisable = sender.Siblings.OfType<MyButtonBase>().Where(b => (b.Model?.V as FileData)?.Equals((FileData)sender.Model.V) == true).ToArray();
            await SetControlStatesAsync(ComponentStateKind.Disabled, btnsToDisable, sender);

            await UploadFileAsync((FileData)sender.Model.V);

            await SetControlStatesAsync(ComponentStateKind.Enabled, btnsToDisable);
        }

        protected async Task BtnRetry_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken _)
        {
            var btnsToDisable = sender.Siblings.OfType<MyButtonBase>().Where(b => (b.Model?.V as FileData)?.Equals((FileData)sender.Model.V) == true).ToArray();
            await SetControlStatesAsync(ComponentStateKind.Disabled, btnsToDisable, sender);

            await UploadFileAsync((FileData)sender.Model.V);

            await SetControlStatesAsync(ComponentStateKind.Enabled, btnsToDisable);
        }

        protected async Task BtnClear_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken token)
        {
            var fd = (FileData)sender.Model.V;
            var btnsToDisable = sender.Siblings.OfType<MyButtonBase>().Where(b => (b.Model?.V as FileData)?.Equals(fd) == true).ToArray();
            await SetControlStatesAsync(ComponentStateKind.Disabled, btnsToDisable, sender);

            Value.Remove(fd);
            await NotifyParametersChangedAsync().StateHasChangedAsync(true);

            await SetControlStatesAsync(ComponentStateKind.Enabled, btnsToDisable);  // for some reason blazor is filling the same button with new parameters instead of creating a new one

            await (await ModuleAsync).InvokeVoidAndCatchCancellationAsync("blazor_FileUpload_RemoveCachedFileUpload", token, _guid, fd.Name, fd.Extension, fd.TotalSizeInBytes);
        }

        protected async Task BtnSelectAll_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken _)
        {
            var select = !Value.All(f => f.IsSelected);
            Value.ForEach(f => f.IsSelected = select);
            await StateHasChangedAsync(true);
        }

        protected async Task BtnUploadMany_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken _)
        {
            var selectedFiles = Files.Any(f => f.IsSelected) ? Files.Where(f => f.IsSelected).ToArray() : Files;
            if (!selectedFiles.Any())
                return;

            await SetControlStateAsync(ComponentStateKind.Loading, sender);

            var filesToUpload = Files.Where(f => f.In(selectedFiles) && f.Status == UploadStatus.NotStarted).ToArray();
            var uploadTasks = new List<Task>();

            foreach (var file in filesToUpload)
            {
                uploadTasks.Add(Task.Run(async () =>
                {
                    var otherBtns = sender.Siblings.OfType<MyButtonBase>().ToArray();
                    var btnsToDisable = otherBtns.Where(b => IsBtnFileEq(b, file)).ToArray();
                    await SetControlStatesAsync(ComponentStateKind.Disabled, btnsToDisable, otherBtns.Single(b => IsBtnFileEq(b, file) && b.Classes.Contains("my-btn-upload-file")));

                    await UploadFileAsync(file);

                    await SetControlStatesAsync(ComponentStateKind.Enabled, btnsToDisable);
                }, _));
            }

            await SetControlStateAsync(ComponentStateKind.Enabled, sender);

            await Task.WhenAll(uploadTasks);
        }

        private FileData GetBtnFileN(MyButtonBase btn) => btn.Model?.V as FileData;
        private bool IsBtnFileIn(MyButtonBase btn, IEnumerable<FileData> fds) => GetBtnFileN(btn)?.In(fds) == true;
        private bool IsBtnFileEq(MyButtonBase btn, FileData fd) => GetBtnFileN(btn)?.Equals(fd) == true;

        //protected string FileDataToCssClass(FileData fd) => $"my-file-{$"{fd.Name}|{fd.Extension}|{fd.TotalSize.SizeInBytes}".UTF8ToBase58()}";

        //protected FileData CssClassToFileData(string cssClass)
        //{
        //    var (name, extension, strSizeInBytes) = cssClass.AfterFirst("my-file-").Base58ToUTF8().Split("|").ToTupleOf3();
        //    var sizeInBytes = strSizeInBytes.ToLong();
        //    return Value.Single(f => f.Name.EqualsInvariant(name) && f.Extension.EqualsInvariant(extension) && f.TotalSize.SizeInBytes == sizeInBytes);
        //}

        private async Task UploadFileAsync(FileData fd)
        {
            fd.Status = UploadStatus.Uploading;
            var jsChunkSize = new FileSize(256, FileSizeSuffix.KB);
            var jsChunkPosition = fd.Position;
            while (!fd.Status.In(UploadStatus.Paused, UploadStatus.Finished, UploadStatus.Failed))
            {
                var chunk = await (await ModuleAsync).InvokeAndCatchCancellationAsync<List<byte>>("blazor_FileUpload_GetFileChunk", _guid, fd.Name, fd.Extension, fd.TotalSizeInBytes, jsChunkPosition, jsChunkSize.SizeInBytes);
                if (chunk is null || chunk.Count == 0)
                    throw new ArgumentException("File chunk seems to be empty");
                fd.Data.AddRange(chunk);
                jsChunkPosition += chunk.Count;

                if (fd.ChunkSize.SizeInBytes >= ChunkSize.V?.SizeInBytes || fd.Position + fd.ChunkSize.SizeInBytes >= fd.TotalSizeInBytes)
                {
                    if (fd.Position + fd.ChunkSize.SizeInBytes > fd.TotalSizeInBytes)
                        throw new ArgumentException("Chunk exceeds file size, it shouldn't happen");

                    IApiResponse uploadChunkResponse = null;
                    if (PredefinedSaveUrl.V == PredefinedSaveUrlKind.SaveFileInUserFolder)
                        uploadChunkResponse = await UploadClient.UploadChunkToUserFolderAsync(fd);
                    if (uploadChunkResponse is null)
                        throw new ArgumentException("Upload method wasn't provided");

                    if (uploadChunkResponse.IsError)
                    {
                        await PromptMessageAsync(NotificationType.Error, "File upload has failed");
                        fd.Status = UploadStatus.Failed;
                        fd.Position = 0;
                    }
                    else
                    {
                        fd.Position += fd.ChunkSize.SizeInBytes;
                        if (fd.Position >= fd.TotalSizeInBytes)
                            fd.Status = UploadStatus.Finished;
                        fd.Data.Clear();
                        await StateHasChangedAsync(true);
                    }
                }

            }

            if (fd.Status.In(UploadStatus.Paused, UploadStatus.Failed))
            {
                fd.Data.Clear();
                await StateHasChangedAsync(true);
            }
        }
    }

    public enum PredefinedSaveUrlKind
    {
        SaveFileInUserFolder
    }
}