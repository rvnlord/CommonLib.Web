using System.Collections.Generic;
using System.ComponentModel;

namespace CommonLib.Web.Source.ViewModels.Admin
{
    public class AdminEditClaimValueVM
    {
        public string Value { get; set; }

        [DisplayName("User Names")]
        public List<string> UserNames { get; set; } = new();
    }
}
