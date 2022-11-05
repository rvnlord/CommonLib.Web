using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace CommonLib.Web.Source.DbContext.Models.Account
{
    public class DbUser : IdentityUser<Guid>
    {
        public string EmailActivationToken { get; set; }

        //public string AvatarHash { get; set; }

        public virtual DbFile Avatar { get; set; } 
        public virtual List<DbFile> Files { get; set; } = new();
    } 
}
