﻿using System;
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
            if (Value is null)
                Value = new List<FileData>();

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
            foreach (var file in Files)
            {
                file.StateChanged -= FileData_StateChanged;
                file.StateChanged += FileData_StateChanged;
            }
        }

        [JSInvokable]
        public async Task AddFilesToUploadAsync(List<FileData> files)
        {
            Value.AddRange(files);
            foreach (var file in files)
            {
                file.StateChanged -= FileData_StateChanged;
                file.StateChanged += FileData_StateChanged;
                //FileData_StateChanged(file, new FileDataStateChangedEventArgs(StatePropertyKind.Status, new OldAndNewValue<bool>(file.IsSelected, file.IsSelected), new OldAndNewValue<UploadStatus>(file.Status, file.Status)));
            }
            
            FileData_StateChanged(null, null);
            await StateHasChangedAsync(true);
        }
        
        protected async Task BtnUpload_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken _)
        {
            await UploadAndManageStateAsync(GetBtnFileN(sender));
        }

        protected async Task BtnPause_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken token)
        {
            await PauseAndManageStateAsync(GetBtnFileN(sender));
        }

        protected async Task BtnResume_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken _)
        {
            await ResumeAndManageStateAsync(GetBtnFileN(sender));
        }

        protected async Task BtnRetry_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken _)
        {
            await RetryAndManageStateAsync(GetBtnFileN(sender));
        }

        protected async Task BtnClear_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken token)
        {
            await ClearAndManageStateAsync(GetBtnFileN(sender));
        }

        protected async Task BtnSelectAll_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken _)
        {
            var select = !Value.All(f => f.IsSelected);
            Value.ForEach(f => f.IsSelected = select);
            await StateHasChangedAsync(true);
        }

        protected async Task BtnUploadMany_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken _)
        {
            var selectedFiles = GetSelectedOrAllFiles();
            if (!selectedFiles.Any())
                return;

            await SetControlStateAsync(ComponentStateKind.Loading, sender);
            var filesToUpload = Files.Where(f => f.In(selectedFiles) && f.Status == UploadStatus.NotStarted).ToArray();
            var uploadTasks = filesToUpload.Select(UploadAndManageStateAsync).ToList();
            await Task.WhenAll(uploadTasks);
        }

        protected async Task BtnPauseMany_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken _)
        {
            var selectedFiles = GetSelectedOrAllFiles();
            if (!selectedFiles.Any())
                return;

            await SetControlStateAsync(ComponentStateKind.Loading, sender);
            var filesToPause = Files.Where(f => f.In(selectedFiles) && f.Status == UploadStatus.Uploading).ToArray();
            var pauseTasks = filesToPause.Select(PauseAndManageStateAsync).ToList();
            await Task.WhenAll(pauseTasks);
        }

        protected async Task BtnResumeMany_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken _)
        {
            var selectedFiles = GetSelectedOrAllFiles();
            if (!selectedFiles.Any())
                return;

            await SetControlStateAsync(ComponentStateKind.Loading, sender);
            var filesToResume = Files.Where(f => f.In(selectedFiles) && f.Status == UploadStatus.Paused).ToArray();
            var resumeTasks = filesToResume.Select(PauseAndManageStateAsync).ToList();
            await Task.WhenAll(resumeTasks);
        }

        protected async Task BtnRetryMany_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken _)
        {
            var selectedFiles = GetSelectedOrAllFiles();
            if (!selectedFiles.Any())
                return;

            await SetControlStateAsync(ComponentStateKind.Loading, sender);
            var filesToRetry = Files.Where(f => f.In(selectedFiles) && f.Status == UploadStatus.Failed).ToArray();
            var retryTasks = filesToRetry.Select(RetryAndManageStateAsync).ToList();
            await Task.WhenAll(retryTasks);
        }

        protected async Task BtnClearMany_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken _)
        {
            var selectedFiles = GetSelectedOrAllFiles();
            if (!selectedFiles.Any())
                return;

            await SetControlStateAsync(ComponentStateKind.Loading, sender);
            var filesToClear = Files.Where(f => f.In(selectedFiles) && f.Status == UploadStatus.Failed).ToArray();
            var clearTasks = filesToClear.Select(ClearAndManageStateAsync).ToList();
            await Task.WhenAll(clearTasks);
        }

        private async void FileData_StateChanged(FileData sender, FileDataStateChangedEventArgs e)
        {
            var btnsForManyFiles = Children.OfType<MyButtonBase>().Where(b => b.Model?.V is null).ToArray();
            var btnChooseFile = btnsForManyFiles.Single(b => b.HasClass("my-btn-choose-file"));
            var btnSelectAllFiles = btnsForManyFiles.Single(b => b.HasClass("my-btn-select-all-files"));
            var btnUploadManyFiles = btnsForManyFiles.Single(b => b.HasClass("my-btn-upload-many-files"));
            var btnPauseManyFiles = btnsForManyFiles.Single(b => b.HasClass("my-btn-pause-many-files"));
            var btnResumeManyFiles = btnsForManyFiles.Single(b => b.HasClass("my-btn-resume-many-files"));
            var btnRetryManyFiles = btnsForManyFiles.Single(b => b.HasClass("my-btn-retry-many-files"));
            var btnClearManyFiles = btnsForManyFiles.Single(b => b.HasClass("my-btn-clear-many-files"));

            var selectedFiles = GetSelectedOrAllFiles();
            var btnsToEnable = new List<MyButtonBase>();
            var btnsToDisable = new List<MyButtonBase>();

            btnsToEnable.Add(btnChooseFile);

            if (selectedFiles.Any()) 
                btnsToEnable.Add(btnSelectAllFiles); 
            else 
                btnsToDisable.Add(btnSelectAllFiles);

            var filesToUpload = selectedFiles.Where(f => f.Status == UploadStatus.NotStarted).ToArray();
            if (filesToUpload.Any())
                btnsToEnable.Add(btnUploadManyFiles); 
            else 
                btnsToDisable.Add(btnUploadManyFiles);

            var filesUploading = selectedFiles.Where(f => f.Status == UploadStatus.Uploading).ToArray();
            if (filesUploading.Any())
                btnsToEnable.Add(btnPauseManyFiles); 
            else 
                btnsToDisable.Add(btnPauseManyFiles);

            var filesPaused = selectedFiles.Where(f => f.Status == UploadStatus.Paused).ToArray();
            if (filesPaused.Any())
                btnsToEnable.Add(btnResumeManyFiles); 
            else 
                btnsToDisable.Add(btnResumeManyFiles);

            var filesFailed = selectedFiles.Where(f => f.Status == UploadStatus.Failed).ToArray();
            if (filesFailed.Any())
                btnsToEnable.Add(btnRetryManyFiles); 
            else 
                btnsToDisable.Add(btnRetryManyFiles);

            var filesToClear = selectedFiles.Where(f => f.Status.In(UploadStatus.Failed, UploadStatus.Paused, UploadStatus.NotStarted, UploadStatus.Finished)).ToArray();
            if (filesToClear.Any())
                btnsToEnable.Add(btnClearManyFiles); 
            else 
                btnsToDisable.Add(btnClearManyFiles);

            await SetControlStatesAsync(ButtonState.Enabled, btnsToEnable);
            await SetControlStatesAsync(ButtonState.Disabled, btnsToDisable);
        }

        private async Task UploadAndManageStateAsync(FileData fd)
        {
            var btnsForTheSameFile = BtnsForFile(fd);
            var btnUpload = BtnForFileByClass(fd, "my-btn-upload-file");
            await SetControlStatesAsync(ComponentStateKind.Disabled, btnsForTheSameFile, btnUpload);
            await UploadFileAsync(fd);
            await SetControlStatesAsync(ComponentStateKind.Enabled, btnsForTheSameFile);
        }

        private async Task PauseAndManageStateAsync(FileData fd)
        {
            var btnPause = BtnForFileByClass(fd, "my-btn-pause-file");
            fd.Status = UploadStatus.Paused;
            await SetControlStateAsync(ComponentStateKind.Loading, btnPause);
        }

        private async Task ResumeAndManageStateAsync(FileData fd)
        {
            var btnsForTheSameFile = BtnsForFile(fd);
            var btnResume = BtnForFileByClass(fd, "my-btn-resume-file");
            await SetControlStatesAsync(ComponentStateKind.Disabled, btnsForTheSameFile, btnResume);
            await UploadFileAsync(fd);
            await SetControlStatesAsync(ComponentStateKind.Enabled, btnsForTheSameFile);
        }

        private async Task RetryAndManageStateAsync(FileData fd)
        {
            var btnsForTheSameFile = BtnsForFile(fd);
            var btnResume = BtnForFileByClass(fd, "my-btn-retry-file");
            await SetControlStatesAsync(ComponentStateKind.Disabled, btnsForTheSameFile, btnResume);
            await UploadFileAsync(fd);
            await SetControlStatesAsync(ComponentStateKind.Enabled, btnsForTheSameFile);
        }

        private async Task ClearAndManageStateAsync(FileData fd)
        {
            var btnsForTheSameFile = BtnsForFile(fd);
            var btnClear = BtnForFileByClass(fd, "my-btn-remove-file");
            await SetControlStatesAsync(ComponentStateKind.Disabled, btnsForTheSameFile, btnClear);
            Value.Remove(fd);
            fd.StateChanged -= FileData_StateChanged;
            await NotifyParametersChangedAsync().StateHasChangedAsync(true);
            await (await ModuleAsync).InvokeVoidAndCatchCancellationAsync("blazor_FileUpload_RemoveCachedFileUpload", _guid, fd.Name, fd.Extension, fd.TotalSizeInBytes);
            //await SetControlStatesAsync(ComponentStateKind.Enabled, btnClear);  // for some reason blazor is filling the same button with new parameters instead of creating a new one
        }

        private FileData GetBtnFileN(MyButtonBase btn) => btn.Model?.V as FileData;
        private bool IsBtnFileIn(MyButtonBase btn, IEnumerable<FileData> fds) => GetBtnFileN(btn)?.In(fds) == true;
        private bool IsBtnFileEq(MyButtonBase btn, FileData fd) => GetBtnFileN(btn)?.Equals(fd) == true;
        private MyButtonBase[] BtnsForFile(FileData fd) => Children.OfType<MyButtonBase>().Where(b => IsBtnFileEq(b, fd)).ToArray();
        private MyButtonBase BtnForFileByClass(FileData fd, string cls) =>  BtnsForFile(fd).Single(b => b.Classes.Contains(cls));
        private IReadOnlyList<FileData> GetSelectedOrAllFiles() => Files.Any(f => f.IsSelected) ? Files.Where(f => f.IsSelected).ToArray() : Files;
        
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
                {
                    await PromptMessageAsync(NotificationType.Error, "File chunk seems to be empty");
                    fd.Status = UploadStatus.Failed;
                    fd.Position = 0;
                    break;
                }

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
                        break;
                    }

                    fd.Position += fd.ChunkSize.SizeInBytes;
                    if (fd.Position >= fd.TotalSizeInBytes)
                        fd.Status = UploadStatus.Finished;
                    fd.Data.Clear();
                    await StateHasChangedAsync(true);
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