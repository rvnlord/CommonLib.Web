using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace CommonLib.Web.Source.DbContext.Models.Account
{
    public class DbUser : IdentityUser<Guid>
    {
        public string EmailActivationToken { get; set; }
        
        public virtual DbFile Avatar { get; set; } 

        public virtual ICollection<IdentityUserClaim<Guid>> Claims { get; set; }
        public virtual ICollection<IdentityUserLogin<Guid>> Logins { get; set; }
        public virtual ICollection<IdentityUserToken<Guid>> Tokens { get; set; }
        public virtual ICollection<IdentityUserRole<Guid>> Roles { get; set; }
        public virtual List<DbFile> Files { get; set; } = new();
    } 
}
