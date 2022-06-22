using System.Threading.Tasks;
using CommonLib.Web.Source.Models;
using CommonLib.Web.Source.Models.Interfaces;
using CommonLib.Web.Source.Services.Interfaces;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Utils.UtilClasses;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace CommonLib.Web.Source.Controllers
{
    [Route("api/backendinfo"), ApiController]
    public class BackendInfoController : ControllerBase
    {
        private readonly IBackendInfoManager _appInfoManager;

        public BackendInfoController(IBackendInfoManager appInfoManager)
        {
            _appInfoManager = appInfoManager;
        }

        [HttpGet("ping")] // GET: api/appinfo/ping
        public async Task<JToken> PingAsync() => await _appInfoManager.PingAsync().ToJTokenAsync();

        [HttpPost("setfrontendbaseurl")] // POST: api/appinfo/setfrontendbaseurl
        public async Task<JToken> SetFrontendBaseUrlAsync(JToken frontEndBaseUrl) => await _appInfoManager.SetFrontendBaseUrlAsync(frontEndBaseUrl.ToString()).ToJTokenAsync(); // TODO: in decentralized app it needs to be set on every req, otherwise we will always have frontend address of the newest peer

        [HttpPost("setbackendbaseurl")] // POST: api/appinfo/setbackendbaseurl
        public async Task<JToken> SetBackendBaseUrlAsync(JToken backEndBaseUrl) => await _appInfoManager.SetBackendBaseUrlAsync(backEndBaseUrl.ToString()).ToJTokenAsync(); 

        [HttpGet("getbackenddbcs")] // POST: api/appinfo/getbackenddbcs
        public async Task<JToken> GetBackendBaseUrlAsync() => await _appInfoManager.GetBackendDBCSAsync().ToJTokenAsync(); // TODO: disable this endpoint on production
    }
}
