using System.Threading.Tasks;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Source.Models.Interfaces;

namespace CommonLib.Web.Source.Services.Upload.Interfaces
{
    public interface IUploadClient
    {
        Task<IApiResponse> UploadChunkToUserFolderAsync(FileData chunk);
    }
}
