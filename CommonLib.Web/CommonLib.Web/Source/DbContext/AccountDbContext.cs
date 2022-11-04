using System;
using System.Linq;
using BlazorDemo.Common.Models.Account;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils;
using CommonLib.Web.Source.Models.Account;
using CommonLib.Source.Common.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CommonLib.Web.Source.DbContext
{
    public class AccountDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        //public StoreOptions StoreOptions { get; set; }
        //public IPersonalDataProtector PersonalDataProtector { get; set; }

        public DbSet<CryptographyKey> CryptographyKeys { get; set; }
        
        public AccountDbContext(DbContextOptions<AccountDbContext> options) : base(options) { }
        
        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);
            mb.RenameIdentityTables();
            mb.Entity<CryptographyKey>().ToTable("CryptographyKeys").HasKey(e => e.Name);
        }

        //protected void IdentityDbContextOnModelCreating(ModelBuilder mb) => base.OnModelCreating(mb);

        public static AccountDbContext Create()
        {
            var o = new DbContextOptionsBuilder<AccountDbContext>();
            o.UseSqlServer(WebUtils.Configuration.GetConnectionString("DBCS"));
            return new AccountDbContext(o.Options);
        }
    }
}
