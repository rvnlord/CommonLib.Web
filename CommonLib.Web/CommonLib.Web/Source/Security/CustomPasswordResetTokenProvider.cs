using System;
using System.Threading.Tasks;
using CommonLib.Web.Source.DbContext;
using CommonLib.Web.Source.Models.Account;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CommonLib.Web.Source.Security
{
    public class CustomPasswordResetTokenProvider<TUser> : IUserTwoFactorTokenProvider<TUser> where TUser : User
    {
        private readonly AccountDbContext _db;
        public DataProtectionTokenProviderOptions Options { get; }
        public IDataProtector Protector { get; }
        public string Name => Options.Name;


        public CustomPasswordResetTokenProvider(
            IDataProtectionProvider dataProtectionProvider, 
            IOptions<CustomPasswordResetTokenProviderOptions> options,
            AccountDbContext db)
        {
            if (dataProtectionProvider == null)
                throw new ArgumentNullException(nameof(dataProtectionProvider));
            Options = options?.Value ?? new DataProtectionTokenProviderOptions();
            Protector = dataProtectionProvider.CreateProtector(Name ?? "DataProtectorTokenProvider"); 

            _db = db;
        }

        public async Task<string> GenerateAsync(string purpose, UserManager<TUser> userManager, TUser user)
        {
            return await CreateDeterministicCodeAsync(userManager, user);
        }

        public async Task<bool> ValidateAsync(string purpose, string encodedToken, UserManager<TUser> userManager, TUser user)
        {
            if (!encodedToken.IsBase58())
                return false;

            var token = encodedToken.Base58ToUTF8();
            if (!token.Contains('|'))
                return false;

            var tokenParts = token.Split("|");
            var securityStamp = tokenParts[0];
            var passwordHash = tokenParts[1];

            return await Task.FromResult(securityStamp.EqualsInvariant(user.SecurityStamp.Take(8)) && user.PasswordHash.StartsWithInvariant(passwordHash));
        }

        public async Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user) => await Task.FromResult(false);

        public async Task<string> GetAsync(string purpose, UserManager<TUser> userManager, TUser user)
        {
            return await CreateDeterministicCodeAsync(userManager, user);
        }

        private async Task<string> CreateDeterministicCodeAsync(UserManager<TUser> userManager, TUser user)
        {
            var securityStamp = (await userManager.GetSecurityStampAsync(user)).Take(8);
            return $"{securityStamp}|{user.PasswordHash}".Take(64).UTF8ToBase58();
        }
    } 

    public class CustomPasswordResetTokenProviderOptions : DataProtectionTokenProviderOptions
    { }
}
