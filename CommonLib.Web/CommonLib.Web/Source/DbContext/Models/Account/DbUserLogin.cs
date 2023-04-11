using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace CommonLib.Web.Source.DbContext.Models.Account
{
    public class DbUserLogin : IdentityUserLogin<Guid>
    {
        public string ExternalName { get; set; }
    }
}
