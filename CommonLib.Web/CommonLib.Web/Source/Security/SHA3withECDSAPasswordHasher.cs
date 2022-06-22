using System;
using System.Linq;
using BlazorDemo.Common.Models.Account;
using CommonLib.Web.Source.DbContext;
using CommonLib.Web.Source.Models.Account;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils;
using Microsoft.AspNetCore.Identity;
using Org.BouncyCastle.Security;

namespace CommonLib.Web.Source.Security
{
public class SHA3withECDSAPasswordHasher<TUser> : IPasswordHasher<TUser> where TUser : User
    {
        private readonly AccountDbContext _db;

        public SHA3withECDSAPasswordHasher(AccountDbContext db)
        {
            _db = db;
        }

        public string HashPassword(TUser user, string password)
        {
            var keyStr = _db.CryptographyKeys.SingleOrDefault(k => k.Name.ToLower() == "PasswordHasher".ToLower())?.Value;
            if (keyStr == null)
            {
                var keys = CryptoUtils.GenerateECKeyPair();
                keyStr = keys.Private.ToECPrivateKeyByteArray().ToBase64SafeUrlString();
                _db.CryptographyKeys.Add(new CryptographyKey { Name = "PasswordHasher", Value = keyStr });
                _db.SaveChanges();
            }

            var ecPrivKey = keyStr?.Base64SafeUrlToByteArray().ECPrivateKeyByteArrayToECPrivateKey();
            var salt = new SecureRandom().GenerateSeed(6);
            var sha3Hash = password.UTF8ToByteArray().Concat(salt).ToArray().Sha3().Concat(salt).ToArray();
            return $"{sha3Hash.ToBase64SafeUrlString()}|{sha3Hash.SignECDSA(ecPrivKey).ToBase64SafeUrlString()}".UTF8ToBase64SafeUrl(); // not meant to increase security as you can see, just foir demonstration purposes that custom cryptography can be used
        }

        public PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword)
        {
            if (hashedPassword == null)
                throw new ArgumentNullException(nameof(hashedPassword));
            if (providedPassword == null)
                return PasswordVerificationResult.Failed;

            var keyStr = _db.CryptographyKeys.SingleOrDefault(k => k.Name.ToLower() == "PasswordHasher".ToLower())?.Value; // if there are passwords in the db and no hasher, just use forgot password form
            if (keyStr == null)
                throw new NullReferenceException("There is no Private Key in the Database");

            var pubKey = keyStr.Base64SafeUrlToByteArray().ECPrivateKeyByteArrayToECPublicKeyByteArray().ECPublicKeyByteArrayToECPublicKey();
            var sha3Hash = hashedPassword.Base64SafeUrlToUTF8().Before("|").Base64SafeUrlToByteArray();
            var ecdsaSignature = hashedPassword.Base64SafeUrlToUTF8().After("|").Base64SafeUrlToByteArray();

            var salt = sha3Hash.TakeLast(6).ToArray();
            var providedPasswordSha3Hash = providedPassword.UTF8ToByteArray().Concat(salt).ToArray().Sha3().Concat(salt).ToArray();

            return providedPasswordSha3Hash.SequenceEqual(sha3Hash) && providedPasswordSha3Hash.VerifyECDSA(ecdsaSignature, pubKey)
                ? PasswordVerificationResult.Success
                : PasswordVerificationResult.Failed;
        }
    }
}
