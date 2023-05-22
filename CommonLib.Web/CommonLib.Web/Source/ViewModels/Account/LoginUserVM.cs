using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CommonLib.Source.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;

namespace CommonLib.Web.Source.ViewModels.Account
{
    public class LoginUserVM
    {
        [DisplayName("User Name")]
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }

        [DisplayName("Remember Me")]
        public bool RememberMe { get; set; }

        public IList<AuthenticationSchemeVM> ExternalLogins { get; set; } = new List<AuthenticationSchemeVM>();
        public string ExternalProvider { get; set; }
        public string ExternalProviderKey { get; set; }
        public string ExternalProviderUserName { get; set; }

        public IList<string> WalletLogins { get; set; } = new List<string>();
        public string WalletProvider { get; set; }
        public string WalletAddress { get; set; }
        public string WalletSignature { get; set; }
        public int WalletChainId { get; set; }

        public string ReturnUrl { get; set; }
        public string Ticket { get; set; }
        public bool IsConfirmed { get; set; }
        public ExternalLoginUsageMode Mode { get; set; }
    }

    public enum ExternalLoginUsageMode
    {
        Connection,
        Login
    }
}
