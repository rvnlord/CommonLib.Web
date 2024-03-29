﻿using System.ComponentModel;

namespace CommonLib.Web.Source.ViewModels.Account
{
    public class ForgotPasswordUserVM
    {
        [DisplayName("User Name")]
        public string UserName { get; set; }
        public string Email { get; set; }
        public string ReturnUrl { get; set; }
    }
}
