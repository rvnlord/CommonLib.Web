using System;
using System.Threading.Tasks;
using CommonLib.Web.Source.DbContext;
using CommonLib.Web.Source.Models.Account;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Security;

namespace CommonLib.Web.Source.Security
{
    public class CustomEmailConfirmationTokenProvider<TUser> : IUserTwoFactorTokenProvider<TUser> where TUser : User
    {
        private readonly AccountDbContext _db;
        public DataProtectionTokenProviderOptions Options { get; }
        public IDataProtector Protector { get; }
        public string Name => Options.Name;


        public CustomEmailConfirmationTokenProvider(
            IDataProtectionProvider dataProtectionProvider, 
            IOptions<CustomEmailConfirmationTokenProviderOptions> options,
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
            var securityStamp = (await userManager.GetSecurityStampAsync(user)).Take(8);
            var timeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var tokenContent = new SecureRandom().GenerateSeed(12).ToBase58String();
            user.EmailActivationToken = $"{timeStamp}|{securityStamp}|{tokenContent}".UTF8ToBase58();
            _db.Users.Update(user);
            await _db.SaveChangesAsync();
            return  user.EmailActivationToken;
        }

        public async Task<bool> ValidateAsync(string purpose, string encodedToken, UserManager<TUser> userManager, TUser user)
        {
            var token = encodedToken.Base58ToUTF8();
            var tokenParts = token.Split("|");
            var timeStamp = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(tokenParts[0]));
            var securityStamp = tokenParts[1];
            var tokenContent = tokenParts[2];

            var dbEncodedToken = user.EmailActivationToken;
            if (!dbEncodedToken.IsBase58())
                return false; // generated using some old method, like base64 token containing `l` char
            var dbToken = dbEncodedToken.Base58ToUTF8();
            var dbTokenParts = dbToken.Split("|");
            var dbTimeStamp = timeStamp + Options.TokenLifespan;
            var dbSecurityStamp = (await userManager.GetSecurityStampAsync(user)).Take(8);
            var dbTokenContent = dbTokenParts[2];

            var isValid =
                encodedToken.EqualsInvariant(dbEncodedToken) && timeStamp < dbTimeStamp && 
                securityStamp.EqualsInvariant(dbSecurityStamp) && tokenContent.EqualsInvariant(dbTokenContent);

            if (isValid)
            {
                user.EmailActivationToken = null;
                _db.Users.Update(user);
                await _db.SaveChangesAsync();
            }

            return isValid;
        }

        public async Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user) => await Task.FromResult(false);
    } 

    public class CustomEmailConfirmationTokenProviderOptions : DataProtectionTokenProviderOptions
    { }
}
