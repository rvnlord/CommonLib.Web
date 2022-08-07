using System;
using System.Threading.Tasks;
using Blazored.SessionStorage;
using CommonLib.Source.Common.Extensions;

namespace CommonLib.Web.Source.Common.Extensions
{
    public static class SessionStorageExtensions
    {
        public static async Task<Guid> GetSessionIdAsync(this ISessionStorageService sessionStorage)
        {
            var strSessId = await sessionStorage.GetItemAsStringAsync("SessionId");
            return strSessId.IsNullOrWhiteSpace() ? throw new NullReferenceException("\"SessionId\" not present") : Guid.Parse(strSessId);
        }

        public static async Task<Guid> GetOrCreateSessionIdAsync(this ISessionStorageService sessionStorage)
        {
            var strSessId = await sessionStorage.GetItemAsStringAsync("SessionId");
            var isSessionIdParsable = Guid.TryParse(strSessId, out var sessionId);

            if (isSessionIdParsable)
                return sessionId;

            sessionId = Guid.NewGuid();
            await sessionStorage.SetItemAsStringAsync("SessionId", sessionId.ToString());
            return sessionId;
        }
    }
}
