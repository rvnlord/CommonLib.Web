using System;
using System.Collections.Generic;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Services.Interfaces;

namespace CommonLib.Web.Source.Services
{
    public class SessionCacheService : Dictionary<Guid, SessionCacheData>, ISessionCacheService
    {
        public SessionCacheService() { }
    }
}
