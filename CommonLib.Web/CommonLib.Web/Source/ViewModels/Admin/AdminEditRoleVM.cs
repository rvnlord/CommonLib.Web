using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace CommonLib.Web.Source.ViewModels.Admin
{
    public class AdminEditRoleVM
    {
        public Guid Id { get; set; }
        
        public string Name { get; set; }

        [DisplayName("User Names")]
        public List<string> UserNames { get; set; } = new();
    }
}
