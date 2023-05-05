using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Source.Models;
using CommonLib.Source.Models.Interfaces;
using CommonLib.Web.Source.Common.Components.MyImageComponent;
using CommonLib.Web.Source.Common.Utils;
using CommonLib.Web.Source.Services.Account.Interfaces;
using CommonLib.Web.Source.Services.Upload.Interfaces;
using CommonLib.Web.Source.Validators.Upload;
using CommonLib.Web.Source.ViewModels.Account;
using Microsoft.Extensions.FileSystemGlobbing;

namespace CommonLib.Web.Source.Services.Upload
{
    public class UploadManager : IUploadManager
    {
        private static string _rootDir;
        private static string _wwwRootDir;
        private static string _commonWwwRootDir;
        private static bool? _isProduction;
        private readonly IAccountManager _accountManager;

        public static string RootDir => _rootDir ??= FileUtils.GetEntryAssemblyDir();
        public static string WwwRootDir => _wwwRootDir ??= ((object) WebUtils.ServerHostEnvironment).GetProperty<string>("WebRootPath");
        public static string CommonWwwRootDir => _commonWwwRootDir ??= FileUtils.GetAspNetWwwRootDir<MyImageBase>();
        public static bool IsProduction => _isProduction ??= Directory.Exists(PathUtils.Combine(PathSeparator.BSlash, WwwRootDir, "_content"));

        public UploadManager(IAccountManager accountManager)
        {
            _accountManager = accountManager;
        }

        public async Task<IApiResponse> UploadChunkToUserFolderAsync(AuthenticateUserVM authUser, FileData chunk)
        {
            authUser = (await _accountManager.GetAuthenticatedUserAsync(null, null, authUser))?.Result;
            if (authUser == null || authUser.AuthenticationStatus != AuthStatus.Authenticated)
                return new ApiResponse(StatusCodeType.Status401Unauthorized, "You are not Authorized to Edit User Data", null);
            chunk.ValidateUploadStatus = false;
            if (!(await new FileSavedToUserFolderValidator().ValidateAsync(chunk.ToListOfOne().ToFileDataList())).IsValid)
                return new ApiResponse(StatusCodeType.Status404NotFound, "Supplied data is invalid", null);
            chunk.ValidateUploadStatus = true;

            var dirToSaveFiles = PathUtils.Combine(PathSeparator.BSlash, FileUtils.GetEntryAssemblyDir(), "UserFiles", authUser.UserName);
            var filePath = PathUtils.Combine(PathSeparator.BSlash, dirToSaveFiles, chunk.NameWithExtension) ?? throw new NullReferenceException();
            var fileExists = File.Exists(filePath);
            if (chunk.Position == 0 && fileExists)
                File.Delete(filePath);
            FileData storedFile = null;
            if (File.Exists(filePath))
            {
                storedFile = filePath.PathToFileData(false);
                storedFile.ValidateUploadStatus = false;
                if (!(await new AvatarValidator(_accountManager).ValidateAsync(storedFile.ToListOfOne().ToFileDataList())).IsValid)
                    return new ApiResponse(StatusCodeType.Status401Unauthorized, "The part of the File stored on server is not valid", null);
            }

            var storedFileSize = storedFile?.TotalSizeInBytes ?? 0;
            if (chunk.Position != 0 && storedFileSize != chunk.Position)
                throw new ArgumentOutOfRangeException(null, "This is not the next part of the file");

            await FileUtils.AppendAllBytesAsync(filePath, chunk.Data.ToArray());
            
            return new ApiResponse(StatusCodeType.Status200OK, "File chunk added to the file", null);
        }

        public async Task<IApiResponse> UploadChunkOfTemporaryAvatarAsync(AuthenticateUserVM authUser, FileData chunk)
        {
            authUser = (await _accountManager.GetAuthenticatedUserAsync(null, null, authUser))?.Result;
            if (authUser == null || authUser.AuthenticationStatus != AuthStatus.Authenticated)
                return new ApiResponse(StatusCodeType.Status401Unauthorized, "You are not Authorized to Edit User Data", null);
            chunk.ValidateUploadStatus = false;
            if (!(await new AvatarValidator(_accountManager).ValidateAsync(chunk.ToListOfOne().ToFileDataList())).IsValid)
                return new ApiResponse(StatusCodeType.Status404NotFound, "Supplied data is invalid", null);
            chunk.ValidateUploadStatus = true;
            
            var tempAvatarDir = PathUtils.Combine(PathSeparator.BSlash, FileUtils.GetEntryAssemblyDir(), "UserFiles", authUser.UserName, "_temp/Avatars");
            var filePath = PathUtils.Combine(PathSeparator.BSlash, tempAvatarDir, chunk.NameWithExtension) ?? throw new NullReferenceException();
            var fileExists = File.Exists(filePath);
            if (chunk.Position == 0 && fileExists)
                File.Delete(filePath);
            FileData storedFile = null;
            if (File.Exists(filePath))
            {
                storedFile = filePath.PathToFileData(false);
                storedFile.ValidateUploadStatus = false;
                if (!(await new AvatarValidator().ValidateAsync(storedFile.ToListOfOne().ToFileDataList())).IsValid)
                    return new ApiResponse(StatusCodeType.Status401Unauthorized, "The part of the Avatar stored on server is not valid", null);
            }
            
            var storedFileSize = storedFile?.TotalSizeInBytes ?? 0;
            if (chunk.Position != 0 && storedFileSize != chunk.Position)
                throw new ArgumentOutOfRangeException(null, "This is not the next part of the file");

            await FileUtils.AppendAllBytesAsync(filePath, chunk.Data.ToArray());
            
            return new ApiResponse(StatusCodeType.Status200OK, "File chunk added to the file", null);
        }

        public async Task<ApiResponse<string>> GetRenderedIconAsync(IconType icon)
        {
            var iconEnums = icon.GetType().GetProperties().Where(p => p.Name.EndsWithInvariant("Icon")).ToArray();
            var iconEnumVals = iconEnums.Select(p => p.GetValue(icon)).ToArray();
            var iconEnum = iconEnumVals.Single(v => v != null);
            var iconType = iconEnum.GetType();
            var iconName = StringConverter.PascalCaseToKebabCase(EnumConverter.EnumToString(iconEnum.CastToReflected(iconType)));
            var iconSetDirName = iconType.Name.BeforeFirst("IconType");
            var iconPath = PathUtils.Combine(PathSeparator.BSlash, RootDir, $@"_myContent\CommonLib.Web\Content\Icons\{iconSetDirName}\{iconName}.svg");
            var svg = (await File.ReadAllTextAsync(iconPath)).TrimMultiline();
            return new ApiResponse<string>(StatusCodeType.Status200OK, "Successfully retrieved icon", null, svg);
        }

        public async Task<ApiResponse<FileData>> GetRenderedImageAsync(string imagePath)
        {
            var fixesImagePath = imagePath.AfterFirstOrWholeIgnoreCase("images\\").AfterFirstOrWholeIgnoreCase("images/");
            var wwwRootPath = PathUtils.Combine(PathSeparator.FSlash, "wwwroot/images", fixesImagePath);
            var wwwRootCommonPath = PathUtils.Combine(PathSeparator.FSlash, "wwwroot/_content/CommonLib.Web/images", fixesImagePath);
            var myContentCommonPath =  PathUtils.Combine(PathSeparator.FSlash, "_myContent/CommonLib.Web/images", fixesImagePath);
            var sourceFilesMatcher = new Matcher().AddInclude(wwwRootPath).AddInclude(wwwRootCommonPath).AddInclude(myContentCommonPath);
            var imgData = sourceFilesMatcher.GetResultsInFullPath(RootDir).FirstOrDefault()?.PathToFileData(true);
            if (imgData is null)
                return await Task.FromResult(new ApiResponse<FileData>(StatusCodeType.Status404NotFound, "File not Found"));
            return await Task.FromResult(new ApiResponse<FileData>(StatusCodeType.Status200OK, "Successfully retrieved image", imgData));
        }
    }
}
