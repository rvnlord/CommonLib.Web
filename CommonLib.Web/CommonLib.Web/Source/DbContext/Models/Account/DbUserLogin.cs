using System;
using Microsoft.AspNetCore.Identity;

namespace CommonLib.Web.Source.DbContext.Models.Account
{
    public class DbUserLogin : IdentityUserLogin<Guid>
    {
        public string ExternalUserName { get; set; }
        
        public virtual DbUser User { get; set; }
    }
}
