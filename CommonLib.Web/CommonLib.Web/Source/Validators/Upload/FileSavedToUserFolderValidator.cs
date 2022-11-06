using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Extensions;
using FluentValidation;

namespace CommonLib.Web.Source.Validators.Upload
{
    public class FileSavedToUserFolderValidator : AbstractValidator<FileDataList>
    {
        public FileSavedToUserFolderValidator()
        {
            RuleFor(m => m)
                .FileSizeWithMessage(fs => fs <= new FileSize(50, FileSizeSuffix.MB))
                .FileExtensionWithMessage(".png", ".jpg", ".bmp", ".gif", ".mkv")
                .FilesUploadedWithMessage()
                .NoFileIsUploadingWithMessage();
        }
    }
}
