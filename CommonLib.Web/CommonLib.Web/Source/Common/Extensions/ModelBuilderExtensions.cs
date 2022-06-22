using System;
using CommonLib.Web.Source.Models.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonLib.Web.Source.Common.Extensions
{
    public static class ModelBuilderExtensions
    {
        public static PropertyBuilder<decimal?> HasPrecision(this PropertyBuilder<decimal?> builder, int precision, int scale)
        {
            return builder.HasColumnType($"decimal({precision},{scale})");
        }

        public static PropertyBuilder<decimal> HasPrecision(this PropertyBuilder<decimal> builder, int precision, int scale)
        {
            return builder.HasColumnType($"decimal({precision},{scale})");
        }

        public static void RenameIdentityTables(this ModelBuilder mb)
        {
            mb.Entity<User>().ToTable("Users");
            mb.Entity<IdentityRole<Guid>>().ToTable("Roles");
            mb.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
            mb.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
            mb.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
            mb.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
            mb.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        }
    }
}
