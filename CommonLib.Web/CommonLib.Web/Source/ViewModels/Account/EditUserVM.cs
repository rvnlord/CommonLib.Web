using System;
using System.ComponentModel;

namespace CommonLib.Web.Source.ViewModels.Account
{
    public class EditUserVM
    {
        public Guid Id { get; set; }
        public string IdString => Id.ToString();
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
        public string Avatar { get; set; }
    }
}
