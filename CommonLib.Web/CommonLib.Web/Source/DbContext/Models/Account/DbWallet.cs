using System;

namespace CommonLib.Web.Source.DbContext.Models.Account
{
    public class DbWallet
    {
        public string Provider { get; set; }
        public string Address { get; set; }

        public Guid? UserId { get; set; }

        public virtual DbUser User { get; set; }
    }
}
