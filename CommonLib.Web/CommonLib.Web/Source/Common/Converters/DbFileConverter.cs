using System;
using System.Linq;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.DbContext.Models.Account;

namespace CommonLib.Web.Source.Common.Converters
{
    public static class DbFileConverter
    {
        public static DbFile ToDbFile(this FileData fd, Guid userOwningFileId, Guid userHavingFileAsAvatarId)
        {
            return new DbFile
            {
                Hash = fd.Hash ?? throw new NullReferenceException("Hash shouldn't be null, it means the file is not completeely loaded"),
                CreationTime = fd.CreationTime.ToUTC().Rfc1123,
                Name = fd.Name,
                Extension = fd.Extension,
                Data = fd.Data.ToArray(),
                UserOwningFileId = userOwningFileId,
                UserHavingFileAsAvatarId = userHavingFileAsAvatarId
            };
        }

        public static FileData ToFileData(this DbFile dbFile)
        {
            return new FileData
            {
                TotalSizeInBytes = dbFile.Data.Length,
                Data = dbFile.Data.ToList(),
                Position = 0,
                Name = dbFile.Name,
                Extension = dbFile.Extension.TrimStart('.'),
                DirectoryPath = null,
                CreationTime = dbFile.CreationTime.ToExtendedTime(),
                IsSelected = false,
                Status = UploadStatus.NotStarted,
                IsPreAdded = false,
                IsExtensionValid = false,
                IsFileSizeValid = false,
                ValidateUploadStatus = false
            };
        }

        public static FileData ToFileDataOrNull(this DbFile dbFile) => dbFile?.ToFileData();
    }
}
