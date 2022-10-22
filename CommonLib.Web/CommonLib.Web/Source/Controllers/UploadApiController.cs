using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Services.Upload.Interfaces;
using CommonLib.Web.Source.ViewModels.Account;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace CommonLib.Web.Source.Controllers
{
    [Route("api/upload"), ApiController]
    public class UploadApiController : MyControllerBase
    {
        private readonly IUploadManager _uploadManager;

        public UploadApiController(IUploadManager uploadManager)
        {
            _uploadManager = uploadManager;
        }

        [HttpPost(nameof(UploadChunkToUserFolderAsync))] // POST: api/upload/UploadChunkToUserFolder
        public async Task<JToken> UploadChunkToUserFolderAsync(JToken JAuthUserAndChunk) => await EnsureVoidResponseAsync(async () => await _uploadManager.UploadChunkToUserFolderAsync(JAuthUserAndChunk["AuthenticatedUser"]?.To<AuthenticateUserVM>(), JAuthUserAndChunk["Chunk"].To<FileData>()));
        
    }
}
