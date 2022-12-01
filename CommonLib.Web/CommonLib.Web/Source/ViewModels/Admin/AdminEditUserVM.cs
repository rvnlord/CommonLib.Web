using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.ViewModels.Account;

namespace CommonLib.Web.Source.ViewModels.Admin
{
    public class AdminEditUserVM
    {
        public Guid Id { get; set; }
        [DisplayName("Id")]
        public string IdString => Id != Guid.Empty ? Id.ToString() : null;
        [DisplayName("User Name")]
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        [DisplayName("Potential Avatars")]
        public FileDataList PotentialAvatars { get; set; }
        [DisplayName("Is Deleted")]
        public bool IsDeleted { get; set; }
        [DisplayName("Is Confirmed")]
        public bool IsConfirmed { get; set; }

        public string ReturnUrl { get; set; }
        public List<FindRoleVM> Roles { get; set; } = new();
        public List<FindClaimVM> Claims { get; set; } = new();
        public string Ticket { get; set; }
        public FileData Avatar { get; set; }

        public bool HasRole(string role) => Roles.Any(r => r.Name.EqualsIgnoreCase(role));
        public bool HasClaim(string claim) => Claims.Any(c => c.Name.EqualsIgnoreCase(claim));
    }
}
