﻿using System;
using System.Collections.Generic;
using System.Linq;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils.UtilClasses;

namespace CommonLib.Web.Source.ViewModels.Account
{
    public class FindUserVM
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public bool IsConfirmed { get; set; }
        public string EmailActivationToken { get; set; }
        public List<FindRoleVM> Roles { get; set; } = new();
        public List<FindClaimVM> Claims { get; set; } = new();
        public FileData Avatar { get; set; }

        public bool HasRole(string role) => Roles.Any(r => r.Name.EqualsIgnoreCase(role));
        public bool HasClaim(string claim) => Claims.Any(c => c.Name.EqualsIgnoreCase(claim));
    }
}
