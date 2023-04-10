using System;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils;
using CommonLib.Web.Source.DbContext.Models.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CommonLib.Web.Source.DbContext
{
    public class AccountDbContext : IdentityDbContext<DbUser, IdentityRole<Guid>, Guid>
    {
        public DbSet<DbCryptographyKey> CryptographyKeys { get; set; }
        public DbSet<DbFile> Files { get; set; }
        public DbSet<DbWallet> Wallets { get; set; }
        
        public AccountDbContext(DbContextOptions<AccountDbContext> options) : base(options) { }
        
        protected override void OnModelCreating(ModelBuilder mb)
        {
            if (mb == null)
                throw new ArgumentNullException(nameof(mb));
            
            base.OnModelCreating(mb);
            mb.RenameIdentityTables();

            mb.Entity<DbUser>()
                .HasMany(e => e.Claims)
                .WithOne()
                .HasForeignKey(uc => uc.UserId)
                .IsRequired();
            
            mb.Entity<DbUser>()
                .HasMany(e => e.Logins)
                .WithOne()
                .HasForeignKey(ul => ul.UserId)
                .IsRequired();
            
            mb.Entity<DbUser>()
                .HasMany(e => e.Tokens)
                .WithOne()
                .HasForeignKey(ut => ut.UserId)
                .IsRequired();
            
            mb.Entity<DbUser>()
                .HasMany(e => e.Roles)
                .WithOne()
                .HasForeignKey(ur => ur.UserId)
                .IsRequired();

            mb.Entity<DbUser>()
                .HasMany(e => e.Files)
                .WithOne(e => e.UserOwningFile)
                .HasForeignKey(e => e.UserOwningFileId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            mb.Entity<DbUser>()
                .HasOne(e => e.Avatar)
                .WithOne(e => e.UserHavingFileAsAvatar)
                .HasForeignKey<DbFile>(e => e.UserHavingFileAsAvatarId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            mb.Entity<DbUser>()
                .HasMany(e => e.Wallets)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            //mb.Entity<DbFile>().Property(e => e.UserHavingFileAsAvatarId).IsRequired(false);
            //mb.Entity<DbUser>(b => b.Navigation(e => e.Avatar).IsRequired());

            mb.Entity<DbCryptographyKey>()
                .ToTable("CryptographyKeys")
                .HasKey(e => e.Name);

            mb.Entity<DbFile>()
                .ToTable("Files")
                .HasKey(e => e.Hash);

            mb.Entity<DbWallet>()
                .ToTable("Wallets")
                .HasKey(e => e.Address);
        }
        
        public static AccountDbContext Create()
        {
            var o = new DbContextOptionsBuilder<AccountDbContext>();
            o.UseSqlServer(WebUtils.Configuration.GetConnectionString("DBCS"));
            return new AccountDbContext(o.Options);
        }
    }
}
