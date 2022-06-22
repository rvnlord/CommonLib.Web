using System;
using Microsoft.AspNetCore.Identity;

namespace CommonLib.Web.Source.Models.Account
{
    public class User : IdentityUser<Guid>
    {
        public string EmailActivationToken { get; set; }
    } 
}
