using System.Threading.Tasks;
using CommonLib.Source.Models;
using CommonLib.Web.Source.Models;
using Newtonsoft.Json.Linq;

namespace CommonLib.Web.Source.Services.Interfaces
{
    public interface IBackendInfoManager
    {
        Task<ApiResponse<JToken>> PingAsync();
        Task<ApiResponse> SetFrontendBaseUrlAsync(string frontendBaseUrl);
        Task<ApiResponse> SetBackendBaseUrlAsync(string backendBaseUrl);
        Task<ApiResponse<string>> GetBackendDBCSAsync();
    }
}
