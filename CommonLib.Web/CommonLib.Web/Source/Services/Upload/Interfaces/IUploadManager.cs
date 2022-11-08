using System.Threading.Tasks;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Source.Models;
using CommonLib.Source.Models.Interfaces;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.ViewModels.Account;

namespace CommonLib.Web.Source.Services.Upload.Interfaces
{
    public interface IUploadManager
    {
        Task<IApiResponse> UploadChunkToUserFolderAsync(AuthenticateUserVM authUser, FileData chunk);
        Task<IApiResponse> UploadChunkOfTemporaryAvatarAsync(AuthenticateUserVM authUser, FileData chunk);
        Task<ApiResponse<string>> GetRenderedIconAsync(IconType icon);
    }
}
