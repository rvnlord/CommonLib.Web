using System;
using System.Collections.Generic;

namespace CommonLib.Web.Source.Services.Interfaces
{
    public interface IComponentsCacheService
    {
        Dictionary<Guid, ComponentsCacheService.SessionData> SessionCache { get; set; }
    }
}
