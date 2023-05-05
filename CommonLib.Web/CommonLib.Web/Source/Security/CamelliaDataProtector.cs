using System;
using System.Linq;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Utils;
using CommonLib.Web.Source.DbContext;
using CommonLib.Web.Source.DbContext.Models.Account;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace CommonLib.Web.Source.Security
{
    public class CamelliaDataProtector : IDataProtector
    {
        private readonly AccountDbContext _db;
        private byte[] _encryptionKey;
        private byte[] _iv;

        public CamelliaDataProtector(AccountDbContext db)
        {
            _db = db;
        }

        public IDataProtector CreateProtector(string purpose)
        {
            var strKey = _db.CryptographyKeys.AsNoTracking().SingleOrDefault(k => k.Name.ToLower() == purpose.ToLower())?.Value;
            if (strKey is null)
            {
                strKey = CryptoUtils.GenerateCamelliaKey().ToBase58String();
                _db.CryptographyKeys.Add(new DbCryptographyKey { Name = purpose, Value = strKey });
                _db.SaveChanges();
            }

            var key = strKey.Base58ToByteArray();
            var keyParam = new KeyParameter(key);
            
            var ivGenerator = new Pkcs5S2ParametersGenerator();
            var salt = RandomUtils.RandomBytes(8).ToArray();
            ivGenerator.Init(key, salt, 1000);
            var parameters = (KeyParameter) ivGenerator.GenerateDerivedMacParameters(128);

            _encryptionKey = keyParam.GetKey();
            _iv = parameters.GetKey();

            return this;
        }

        public byte[] Protect(byte[] plaintext)
        {
            var cipher = new BufferedBlockCipher(new CfbBlockCipher(new CamelliaEngine(), 128));
            cipher.Init(true, new ParametersWithIV(new KeyParameter(_encryptionKey), _iv));
            var output = new byte[cipher.GetOutputSize(plaintext.Length)];
            var length = cipher.ProcessBytes(plaintext, 0, plaintext.Length, output, 0);
            cipher.DoFinal(output, length);

            var ivAndCiphertext = new byte[_iv.Length + output.Length];
            Buffer.BlockCopy(_iv, 0, ivAndCiphertext, 0, _iv.Length);
            Buffer.BlockCopy(output, 0, ivAndCiphertext, _iv.Length, output.Length);

            return ivAndCiphertext;
        }

        public byte[] Unprotect(byte[] protectedData)
        {
            var cipher = new BufferedBlockCipher(new CfbBlockCipher(new CamelliaEngine(), 128));
            cipher.Init(false, new ParametersWithIV(new KeyParameter(_encryptionKey), _iv));
            var output = new byte[cipher.GetOutputSize(protectedData.Length - _iv.Length)];
            var length = cipher.ProcessBytes(protectedData, _iv.Length, protectedData.Length - _iv.Length, output, 0);
            cipher.DoFinal(output, length);

            return output;
        }
    }
}
