using System;
using System.Collections.Generic;
using CommonLib.Web.Source.Common.Utils.UtilClasses;

namespace CommonLib.Web.Source.Services.Interfaces
{
    public interface ISessionCacheService : IDictionary<Guid, SessionCacheData>
    {
        
    }
}
