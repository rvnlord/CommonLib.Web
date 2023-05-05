using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.DbContext;
using Microsoft.AspNetCore.DataProtection;

namespace CommonLib.Web.Source.Security
{
    public class CamelliaDataProtectionProvider : IDataProtectionProvider
    {
        private readonly CamelliaDataProtector _cdp;

        public CamelliaDataProtectionProvider()
        {
            _cdp = new CamelliaDataProtector(new AccountDbContext(DbContextFactory.GetMSSQLDbContextOptions<AccountDbContext>()));
        }

        public CamelliaDataProtectionProvider(AccountDbContext db)
        {
            _cdp = new CamelliaDataProtector(db);
        }

        public IDataProtector CreateProtector(string purpose) => _cdp.CreateProtector(purpose);
        public byte[] Protect(byte[] plainData) => _cdp.Protect(plainData);
        public byte[] Unprotect(byte[] cipheredData) => _cdp.Unprotect(cipheredData);
    }
}
