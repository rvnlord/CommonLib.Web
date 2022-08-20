using System;

namespace CommonLib.Web.Source.Services
{
    public interface IRequestScopedCacheService
    {
        public Guid TemporarySessionId { get; set; } 
    }
}
