using System;
using System.Linq;
using System.Threading.Tasks;
using CommonLib.Web.Source.DbContext;
using CommonLib.Web.Source.Models;
using CommonLib.Web.Source.Models.Interfaces;
using CommonLib.Web.Source.Services.Interfaces;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Source.Models;
using CommonLib.Web.Source.DbContext.Models.Account;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace CommonLib.Web.Source.Services
{
    public class BackendInfoManager : IBackendInfoManager
    {
        private readonly AccountDbContext _db;
        public IConfiguration Configuration { get; }

        public BackendInfoManager(IConfiguration conf, AccountDbContext db)
        {
            _db = db;
            Configuration = conf;
        }

        public async Task<ApiResponse<JToken>> PingAsync()
        {
            try
            {
                return await Task.FromResult(new ApiResponse<JToken>(StatusCodeType.Status200OK, "Server Pinged Successfully", null, new JObject { ["TimeStamp"] = ExtendedTime.UtcNow.ToString() }));
            }
            catch (Exception ex)
            {
                return new ApiResponse<JToken>(StatusCodeType.Status500InternalServerError, "Failed Setting Back End Base url Value", null, null, ex);
            }
        }

        public async Task<ApiResponse> SetFrontendBaseUrlAsync(string frontendBaseUrl)
        {
            try
            {
                ConfigUtils.FrontendBaseUrl = frontendBaseUrl;
                return await Task.FromResult(new ApiResponse(StatusCodeType.Status200OK, "Front End Base url Value Set Successfully", null));
            }
            catch (Exception ex)
            {
                return new ApiResponse(StatusCodeType.Status500InternalServerError, "Failed Setting Front End Base url Value", null, null, ex);
            }
        }

        public async Task<ApiResponse> SetBackendBaseUrlAsync(string backendBaseUrl)
        {
            try
            {
                ConfigUtils.BackendBaseUrl = backendBaseUrl;
                return await Task.FromResult(new ApiResponse(StatusCodeType.Status200OK, "Back End Base url Value Set Successfully", null));
            }
            catch (Exception ex)
            {
                return new ApiResponse(StatusCodeType.Status500InternalServerError, "Failed Setting Back End Base url Value", null, null, ex);
            }
        }

        public async Task<ApiResponse<string>> GetBackendDBCSAsync()
        {
            try
            {
                var key = (await _db.CryptographyKeys.AsNoTracking().SingleOrDefaultAsync(k => k.Name.ToLower() == "DBCS"))?.Value;
                if (key == null)
                {
                    key = CryptoUtils.GenerateCamelliaKey().ToBase58String();
                    await _db.CryptographyKeys.AddAsync(new DbCryptographyKey { Name = "DBCS", Value = key });
                    await _db.SaveChangesAsync();
                }

                var dbcs = Configuration.GetConnectionString("DBCS").UTF8ToByteArray().EncryptCamellia(key.Base58ToByteArray()).ToBase58String();
                return await Task.FromResult(new ApiResponse<string>(StatusCodeType.Status200OK, "Back End Base url Value Set Successfully", null, dbcs));
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>(StatusCodeType.Status500InternalServerError, "Failed Setting Back End Base url Value", null, null, ex);
            }
        }
    }
}
