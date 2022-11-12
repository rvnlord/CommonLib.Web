using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();
        public string ExternalProvider { get; set; }

        public string ReturnUrl { get; set; }
        public string ExternalProviderKey { get; set; }
        public string Ticket { get; set; }
        public bool IsConfirmed { get; set; }
    }
}
