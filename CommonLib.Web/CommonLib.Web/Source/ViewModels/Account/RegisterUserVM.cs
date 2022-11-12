using System;
using System.ComponentModel;

namespace CommonLib.Web.Source.ViewModels.Account
{
    public class RegisterUserVM : BaseVM
    {
        public Guid? Id { get; set; }
        [DisplayName("User Name")]
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        [DisplayName("Confirm Password")]
        public string ConfirmPassword { get; set; }
        public string ReturnUrl { get; set; }
        public string Ticket { get; set; }
    }
}
