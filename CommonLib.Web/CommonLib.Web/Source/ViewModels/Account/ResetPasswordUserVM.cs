using System.ComponentModel;

namespace CommonLib.Web.Source.ViewModels.Account
{
    public class ResetPasswordUserVM
    {
        [DisplayName("User Name")]
        public string UserName { get; set; }
        public string Email { get; set; }

        [DisplayName("Reset Password Code")]
        public string ResetPasswordCode { get; set; }
        
        public string Password { get; set; }

        [DisplayName("Confirm Password")]
        public string ConfirmPassword { get; set; }

        public string ReturnUrl { get; set; }
    }
}
