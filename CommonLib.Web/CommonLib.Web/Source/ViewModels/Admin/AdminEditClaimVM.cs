using System.Collections.Generic;
using System.Linq;

namespace CommonLib.Web.Source.ViewModels.Admin
{
    public class AdminEditClaimVM
    {
        public string OriginalName { get; set; } // to validate against since we don't have Id

        public string Name { get; set; }

        public List<AdminEditClaimValueVM> Values { get; set; } = new();

        public List<string> GetUserNames() => Values.SelectMany(cv => cv.UserNames).OrderBy(n => n).ToList();
    }
}
