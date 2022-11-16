using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
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

        [HttpPost(nameof(UploadChunkToUserFolderAsync))] // POST: api/upload/UploadChunkToUserFolderAsync
        public async Task<JToken> UploadChunkToUserFolderAsync(JToken JAuthUserAndChunk) => await EnsureVoidResponseAsync(async () => await _uploadManager.UploadChunkToUserFolderAsync(JAuthUserAndChunk["AuthenticatedUser"]?.To<AuthenticateUserVM>(), JAuthUserAndChunk["Chunk"].To<FileData>()));
        
        [HttpPost(nameof(UploadChunkOfTemporaryAvatarAsync))] // POST: api/upload/UploadChunkOfTemporaryAvatarAsync
        public async Task<JToken> UploadChunkOfTemporaryAvatarAsync(JToken JAuthUserAndChunk) => await EnsureVoidResponseAsync(async () => await _uploadManager.UploadChunkOfTemporaryAvatarAsync(JAuthUserAndChunk["AuthenticatedUser"]?.To<AuthenticateUserVM>(), JAuthUserAndChunk["Chunk"].To<FileData>()));

        [HttpPost(nameof(GetRenderedIconAsync))] // POST: api/upload/GetRenderedIconAsync
        public async Task<JToken> GetRenderedIconAsync(JToken jIconType) => await EnsureResponseAsync(async () => await _uploadManager.GetRenderedIconAsync(jIconType?.To<IconType>()));

        [HttpPost(nameof(GetRenderedImageAsync))] // POST: api/upload/GetRenderedImageAsync
        public async Task<JToken> GetRenderedImageAsync(JToken jImagePath) => await EnsureResponseAsync(async () => await _uploadManager.GetRenderedImageAsync(jImagePath is JValue ? jImagePath.ToString() : jImagePath["ImagePath"]?.ToString()));

    }
}
