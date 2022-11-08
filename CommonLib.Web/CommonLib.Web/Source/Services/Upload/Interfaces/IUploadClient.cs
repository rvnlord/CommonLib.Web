using System.Threading.Tasks;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Source.Models;
using CommonLib.Source.Models.Interfaces;
using CommonLib.Web.Source.Common.Utils.UtilClasses;

namespace CommonLib.Web.Source.Services.Upload.Interfaces
{
    public interface IUploadClient
    {
        Task<IApiResponse> UploadChunkToUserFolderAsync(FileData chunk);
        Task<IApiResponse> UploadChunkOfTemporaryAvatarAsync(FileData chunk);
        Task<ApiResponse<string>> GetRenderedIconAsync(IconType icon);
    }
}
