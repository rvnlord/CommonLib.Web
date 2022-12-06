using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace CommonLib.Web.Source.ViewModels.Admin
{
    public class AdminEditRoleVM
    {
        public Guid Id { get; set; }
        [DisplayName("Id")] public string IdString => Id != Guid.Empty ? Id.ToString() : null;

        public string Name { get; set; }

        [DisplayName("User Names")]
        public List<string> UserNames { get; set; } = new();
    }
}
