using System.ComponentModel;

namespace CommonLib.Web.Source.ViewModels.Account
{
    public class ConfirmUserVM
    {
        [DisplayName("User Name")]
        public string UserName { get; set; }
        public string Email { get; set; }
        [DisplayName("Confirmation Code")]
        public string ConfirmationCode { get; set; }
        public string ReturnUrl { get; set; }
    }
}
