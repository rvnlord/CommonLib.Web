using System;
using System.Collections.Generic;

namespace CommonLib.Web.Source.DbContext.Models.Account
{
    public class DbFile
    {
        public string Hash { get; set; }
        public DateTime CreationTime { get; set; }
        public string Name { get; set; }
        public string Extension { get; set; }
        public byte[] Data { get; set; }

        public Guid UserOwningFileId { get; set; } // must be sb's file
        public Guid? UserHavingFileAsAvatarId { get; set; } // may be sb's avatar

        public virtual DbUser UserOwningFile { get; set; }
        public virtual DbUser UserHavingFileAsAvatar { get; set; }
    }
}
