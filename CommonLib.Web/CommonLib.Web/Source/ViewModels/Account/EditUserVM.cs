using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using CommonLib.Source.Common.Utils.UtilClasses;

namespace CommonLib.Web.Source.ViewModels.Account
{
    public class EditUserVM
    {
        public Guid Id { get; set; }
        [DisplayName("Id")]
        public string IdString => Id != Guid.Empty ? Id.ToString() : null;
        [DisplayName("User Name")]
        public string UserName { get; set; }
        public string Email { get; set; }
        [DisplayName("Old Password")]
        public string OldPassword { get; set; }
        public bool HasPassword { get; set; }
        [DisplayName("New Password")]
        public string NewPassword { get; set; }
        [DisplayName("Confirm New Password")]
        public string ConfirmNewPassword { get; set; }
        public string Ticket { get; set; }
        public string ReturnUrl { get; set; }
        public FileData Avatar { get; set; }
        public bool ShouldLogout { get; set; }
        [DisplayName("Potential Avatars")]
        public FileDataList PotentialAvatars { get; set; }

        public List<ExternalLoginVM> ExternalLogins { get; set; } = new();
        public string ExternalProviderToDisconnect { get; set; }
        public List<WalletVM> Wallets { get; set; } = new();
        public string WalletProviderToConnect { get; set; }
        [DisplayName("On Chain Status")]
        public bool IsOnChain { get; set; }
        [DisplayName("Deploying Wallet")]
        public WalletVM DeployingWallet { get; set; }
    }
}
