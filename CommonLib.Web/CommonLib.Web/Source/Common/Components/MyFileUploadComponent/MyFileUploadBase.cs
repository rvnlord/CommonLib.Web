using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Source.Models.Interfaces;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyEditFormComponent;
using CommonLib.Web.Source.Common.Components.MyFluentValidatorComponent;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Components.MyPromptComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Models;
using CommonLib.Web.Source.Services.Interfaces;
using CommonLib.Web.Source.Services.Upload.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MoreLinq;
using Newtonsoft.Json.Linq;
using Truncon.Collections;

namespace CommonLib.Web.Source.Common.Components.MyFileUploadComponent
{
    public class MyFileUploadBase : MyInputBase<FileDataList>
    {
        private readonly OrderedSemaphore _syncFileDataState = new(1, 1);
        private int _multipleFileBtnRenders;
        private FileData _prevNoThumbnailImage;
        private MyFluentValidatorBase _validator => Ancestors.OfType<MyEditFormBase>().FirstOrDefault()?.Children.OfType<MyFluentValidatorBase>()?.FirstOrDefault();

        protected OrderedDictionary<string, string> _thumbnailContainerStyle { get; } = new();
        protected string _thumbnailContainerRenderStyle => _thumbnailContainerStyle.CssDictionaryToString();
        protected BlazorParameter<bool?> _inheritState { get; set; }

        public IReadOnlyList<FileData> Files => Value?.ToList() ?? new List<FileData>();
        public IReadOnlyList<FileData> ValidFiles => Value?.Where(f => f.IsFileSizeValid && f.IsExtensionValid || f.IsPreAdded).ToList() ?? new List<FileData>(); // without checking uploaded status

        [Parameter]
        public BlazorParameter<Expression<Func<FileData>>> PreviewFor { get; set; }

        [Parameter]
        public BlazorParameter<FileSize?> ChunkSize { get; set; }

        [Parameter]
        public BlazorParameter<string> SaveUrl { get; set; }

        [Parameter]
        public BlazorParameter<PredefinedSaveUrlKind?> PredefinedSaveUrl { get; set; }

        [Inject]
        public IUploadClient UploadClient { get; set; }

        [Inject]
        public IJQueryService JQuery { get; set; }

        protected override Task OnInitializedAsync()
        {
            _inheritState = false;
            return Task.CompletedTask;
        }

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
            if (For is not null && Model is not null)
                (_, _propName, Value, displayName) = For.GetModelAndProperty();
            if (Value is null)
            {
                Model.SetPropertyValue(_propName, new FileDataList());
                Value = Model.GetPropertyValue<FileDataList>(_propName);
            }

            Placeholder = !Placeholder.IsNullOrWhiteSpace() ? Placeholder : !displayName.IsNullOrWhiteSpace() ? $"{displayName}..." : null;

            if (State.HasChanged())
            {
                State.ParameterValue ??= InputState.Disabled;
                var thumbnailContainerSelector = $"div.my-fileupload[my-guid='{_guid}'] > .my-fileupload-thumbnail-container";
                if (State.ParameterValue.IsDisabledOrForceDisabled)
                {
                    AddAttribute("disabled", string.Empty);
                    AddClass("disabled");
                    if (PreviewFor.HasValue() || PreviewFor?.V?.Compile()?.Invoke() == _prevNoThumbnailImage)
                    {
                        if (!IsRendered)
                            _thumbnailContainerStyle.AddOrUpdate("opacity", "0.3");
                        else
                            await JQuery.QueryOneAsync(thumbnailContainerSelector).CssAsync("opacity", "0.3");
                    }
                }
                else
                {
                    RemoveAttribute("disabled");
                    RemoveClass("disabled");

                    if (!IsRendered)
                        _thumbnailContainerStyle.RemoveIfExists("opacity");
                    else
                        await JQuery.QueryOneAsync(thumbnailContainerSelector).RemoveCssAsync("opacity");
                }
            }

            if (ChunkSize.HasChanged())
                ChunkSize.ParameterValue ??= new FileSize(2, FileSizeSuffix.MB);

            if (PredefinedSaveUrl.HasChanged() || SaveUrl.HasChanged())
            {
                if (PredefinedSaveUrl.HasValue() && SaveUrl.HasValue() || !PredefinedSaveUrl.HasValue() && !SaveUrl.HasValue())
                    throw new ArgumentException("Upload controller should be either predefined or have a defined upload url but not both");
            }

            if (Validate.HasChanged())
                Validate.ParameterValue ??= true;

            CascadedEditContext.BindValidationStateChanged(CurrentEditContext_ValidationStateChangedAsync);
        }

        protected override async Task OnAfterFirstRenderAsync()
        {
            await (await ModuleAsync).InvokeVoidAndCatchCancellationAsync("blazor_FileUpload_AfterFirstRender", _guid, DotNetObjectReference.Create(this));
            foreach (var file in Files)
            {
                file.IsPreAdded = true;
                file.StateChanged -= FileData_StateChanged;
                file.StateChanged += FileData_StateChanged;
            }

            // can't enable some buttons directly here because every control should wait for being explicitly enabled after page render
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
                return;

            var noThumbnailFd = PreviewFor?.V?.Compile().Invoke();
            if (noThumbnailFd != _prevNoThumbnailImage)
                await SetMultipleFileBtnsStateAsync(FileData.Empty);
            else
                await SetMultipleFileBtnsStateAsync(null, true);
        }

        [JSInvokable]
        public async Task AddFilesToUploadAsync(List<FileData> files)
        {
            Value.AddRange(files);
            Value.ForEach(fd => fd.ValidateUploadStatus = false);
            var isValid = _validator is null || await _validator.ValidateFieldAsync(new FieldIdentifier(Model, _propName), false);
            var addedFiles = files.Where(f => f.IsValid).ToArray();
            if (!isValid)
            {
                var invalidFiles = Value.Where(f => !f.IsValid && !f.IsPreAdded).ToArray();
                if (!addedFiles.Any())
                {
                    await CascadedEditContext.V.NotifyFieldChangedAsync(new FieldIdentifier(Model, _propName), true);
                    Value.RemoveRange(invalidFiles);
                }
                else
                {
                    Value.RemoveRange(invalidFiles);
                    await CascadedEditContext.V.NotifyFieldChangedAsync(new FieldIdentifier(Model, _propName), true);
                    await PromptMessageAsync(NotificationType.Warning, "Some files were invalid and have not been added");
                }

                var jInvalidFiles = invalidFiles.Select(f => new JObject
                {
                    [nameof(f.Name)] = f.Name,
                    [nameof(f.Extension)] = f.Extension,
                    [nameof(f.TotalSizeInBytes)] = f.TotalSizeInBytes
                }).ToJToken();
                await (await ModuleAsync).InvokeVoidAndCatchCancellationAsync("blazor_FileUpload_RemoveCachedFileUploads", _guid, jInvalidFiles.JsonSerialize());
            }
            else
            {
                await CascadedEditContext.V.NotifyFieldChangedAsync(new FieldIdentifier(Model, _propName), true);
            }

            Value.ForEach(fd => fd.ValidateUploadStatus = true);

            if (addedFiles.Any())
            {
                foreach (var file in addedFiles)
                {
                    file.StateChanged -= FileData_StateChanged;
                    file.StateChanged += FileData_StateChanged;
                }
                await SetMultipleFileBtnsStateAsync(null);
            }
        }

        protected async Task BtnUpload_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken _)
        {
            var fileToUpload = GetBtnFileN(sender);
            await SetThumbnailAsync(fileToUpload);
            await UploadAndManageStateAsync(fileToUpload);
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
            var fd = GetBtnFileN(sender);
            await ClearAndManageStateAsync(fd);
            var selectedFiles = GetSelectedOrAllFiles();
            var fileToPreview = selectedFiles.Any() ? selectedFiles[^1] : FileData.Empty;
            await SetMultipleFileBtnsStateAsync(fileToPreview); // if method calls UploadFileAsync(), state of the buttons controlling many files will be taken care of because its change is called on every FileData statee change
            Value.ForEach(f => f.ValidateUploadStatus = false);
            await _validator.ValidateFieldAsync(new FieldIdentifier(Model, _propName));
            Value.ForEach(f => f.ValidateUploadStatus = true);
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
            var resumeTasks = filesToResume.Select(ResumeAndManageStateAsync).ToList();
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
            var filesToClear = Files.Where(f => f.In(selectedFiles) && f.Status.In(UploadStatus.Failed, UploadStatus.Paused, UploadStatus.Finished, UploadStatus.NotStarted)).ToArray();
            var clearTasks = filesToClear.Select(ClearAndManageStateAsync).ToList();
            await Task.WhenAll(clearTasks);
            selectedFiles = GetSelectedOrAllFiles();
            var fileToPreview = selectedFiles.Any() ? selectedFiles[^1] : FileData.Empty;
            await SetMultipleFileBtnsStateAsync(fileToPreview);
            Value.ForEach(fd => fd.ValidateUploadStatus = false);
            await _validator.ValidateFieldAsync(new FieldIdentifier(Model, _propName));
            Value.ForEach(fd => fd.ValidateUploadStatus = true);
        }

        private async void FileData_StateChanged(FileData sender, FileDataStateChangedEventArgs e)
        {
            FileData fileToPreview = null;
            if (e.Property == StatePropertyKind.IsSelected)
            {
                if (e.IsSelected.NewValue)
                    fileToPreview = sender;
                else
                {
                    var selectedFiles = GetSelectedOrAllFiles();
                    fileToPreview = selectedFiles.Any() ? selectedFiles[^1] : FileData.Empty;
                }
            }

            await SetMultipleFileBtnsStateAsync(fileToPreview);
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
        private MyButtonBase BtnForFileByClass(FileData fd, string cls) => BtnsForFile(fd).Single(b => b.Classes.Contains(cls));
        private IReadOnlyList<FileData> GetSelectedOrAllFiles() => Files.Any(f => f.IsSelected) ? Files.Where(f => f.IsSelected).ToArray() : Files;

        internal async Task SetMultipleFileBtnsStateAsync(FileData fileToPreview, bool changeOnlyBtnsState = false)
        {
            if (!changeOnlyBtnsState)
                _multipleFileBtnRenders++;
            await _syncFileDataState.WaitAsync();
            if (!changeOnlyBtnsState)
                _multipleFileBtnRenders--;

            try
            {
                if (IsDisposed)
                    return;

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

                if (State.V != InputState.Disabled)
                    btnsToEnable.Add(btnChooseFile);
                else
                    btnsToDisable.Add(btnChooseFile);

                if (selectedFiles.Any() && State.V != InputState.Disabled)
                    btnsToEnable.Add(btnSelectAllFiles);
                else
                    btnsToDisable.Add(btnSelectAllFiles);

                var filesToUpload = selectedFiles.Where(f => f.Status == UploadStatus.NotStarted).ToArray();
                if (filesToUpload.Any() && State.V != InputState.Disabled)
                    btnsToEnable.Add(btnUploadManyFiles);
                else
                    btnsToDisable.Add(btnUploadManyFiles);

                var filesUploading = selectedFiles.Where(f => f.Status == UploadStatus.Uploading).ToArray();
                if (filesUploading.Any() && State.V != InputState.Disabled)
                    btnsToEnable.Add(btnPauseManyFiles);
                else
                    btnsToDisable.Add(btnPauseManyFiles);

                var filesPaused = selectedFiles.Where(f => f.Status == UploadStatus.Paused).ToArray();
                if (filesPaused.Any() && State.V != InputState.Disabled)
                    btnsToEnable.Add(btnResumeManyFiles);
                else
                    btnsToDisable.Add(btnResumeManyFiles);

                var filesFailed = selectedFiles.Where(f => f.Status == UploadStatus.Failed).ToArray();
                if (filesFailed.Any() && State.V != InputState.Disabled)
                    btnsToEnable.Add(btnRetryManyFiles);
                else
                    btnsToDisable.Add(btnRetryManyFiles);

                var filesToClear = selectedFiles.Where(f => f.Status.In(UploadStatus.Failed, UploadStatus.Paused, UploadStatus.NotStarted, UploadStatus.Finished)).ToArray();
                if (filesToClear.Any() && State.V != InputState.Disabled)
                    btnsToEnable.Add(btnClearManyFiles);
                else
                    btnsToDisable.Add(btnClearManyFiles);

                await SetControlStatesAsync(ButtonState.Enabled, btnsToEnable, null, false);
                await SetControlStatesAsync(ButtonState.Disabled, btnsToDisable, null, false);
                if (changeOnlyBtnsState)
                {
                    var tasksBtnsChangeState = btnsToEnable.Concat(btnsToDisable).Select(btn => btn.StateHasChangedAsync(true)).ToArray();
                    await Task.WhenAll(tasksBtnsChangeState);
                }
                else if (_multipleFileBtnRenders == 0)
                    await StateHasChangedAsync(true);
                if (fileToPreview is not null)
                    await SetThumbnailAsync(fileToPreview);
            }
            finally
            {
                await _syncFileDataState.ReleaseAsync();
            }
        }

        private async Task SetThumbnailAsync(FileData fd)
        {
            var noThumbnailFd = PreviewFor?.V?.Compile().Invoke();
            var noThumbnailImage = noThumbnailFd?.ToBase64ImageString();
            if (noThumbnailImage is not null)
                _prevNoThumbnailImage = noThumbnailFd;
            
            await (await ModuleAsync).InvokeVoidAndCatchCancellationAsync("blazor_FileUpload_SetThumbnail", _guid, fd.Name, fd.Extension, fd.TotalSizeInBytes, noThumbnailImage);
        }

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
                    if (PredefinedSaveUrl.V == PredefinedSaveUrlKind.SaveTemporaryAvatar)
                        uploadChunkResponse = await UploadClient.UploadChunkOfTemporaryAvatarAsync(fd);
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
        SaveFileInUserFolder,
        SaveTemporaryAvatar
    }
}