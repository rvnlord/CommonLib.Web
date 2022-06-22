using System.Net.Http;
using System.Threading.Tasks;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Source.Models;
using CommonLib.Web.Source.Models;

namespace CommonLib.Web.Source.Services.Interfaces
{
    public interface IBackendInfoClient
    {
        HttpClient HttpClient { get; set; }

        Task<ApiResponse<ExtendedTime>> PingAsync();
        Task<ApiResponse> SetFrontendBaseUrlAsync(string frontendBaseUrl);
        Task<ApiResponse> SetBackendBaseUrlAsync(string backendBaseUrl);
        Task<ApiResponse<string>> GetBackendDBCSAsync();
    }
}
