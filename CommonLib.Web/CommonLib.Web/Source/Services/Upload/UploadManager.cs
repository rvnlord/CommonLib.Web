using System;
using System.IO;
using System.Threading.Tasks;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Source.Models;
using CommonLib.Source.Models.Interfaces;
using CommonLib.Web.Source.Services.Account.Interfaces;
using CommonLib.Web.Source.Services.Upload.Interfaces;
using CommonLib.Web.Source.Validators.Upload;
using CommonLib.Web.Source.ViewModels.Account;

namespace CommonLib.Web.Source.Services.Upload
{
    public class UploadManager : IUploadManager
    {
        private readonly IAccountManager _accountManager;

        public UploadManager(IAccountManager accountManager)
        {
            _accountManager = accountManager;
        }

        public async Task<IApiResponse> UploadChunkToUserFolderAsync(AuthenticateUserVM authUser, FileData fileData)
        {
            authUser = (await _accountManager.GetAuthenticatedUserAsync(null, null, authUser))?.Result;
            if (authUser == null || authUser.AuthenticationStatus != AuthStatus.Authenticated)
                return new ApiResponse(StatusCodeType.Status401Unauthorized, "You are not Authorized to Edit User Data", null);
            if (!(await new FileChunkSavedToUserFolderValidator(_accountManager).ValidateAsync(fileData)).IsValid)
                return new ApiResponse(StatusCodeType.Status404NotFound, "Supplied data is invalid", null);

            var dirToSaveFiles = PathUtils.Combine(PathSeparator.BSlash, FileUtils.GetEntryProjectDir(), "UserFiles", authUser.UserName);
            var filePath = PathUtils.Combine(PathSeparator.BSlash, dirToSaveFiles, fileData.NameWithExtension) ?? throw new NullReferenceException();
            var fileExists = File.Exists(filePath);
            if (fileData.Position == 0 && fileExists)
                File.Delete(filePath);
            var storedFileSize = File.Exists(filePath) ? new FileInfo(filePath).Length : 0;
            if (fileData.Position != 0 && storedFileSize != fileData.Position)
                throw new ArgumentOutOfRangeException(null, "This is not the next part of the file");

            await FileUtils.AppendAllBytesAsync(filePath, fileData.Data.ToArray());
            
            return new ApiResponse(StatusCodeType.Status200OK, "File chunk added to the file", null);
        }
    }
}
