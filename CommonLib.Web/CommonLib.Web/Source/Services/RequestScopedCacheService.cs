using System;

namespace CommonLib.Web.Source.Services
{
    public class RequestScopedCacheService : IRequestScopedCacheService
    {
        public Guid TemporarySessionId { get; set; } 
    }
}
