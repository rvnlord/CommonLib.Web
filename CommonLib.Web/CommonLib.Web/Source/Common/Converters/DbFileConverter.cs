using System;
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
    }
}
