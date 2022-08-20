using System;
using System.Threading.Tasks;
using Blazored.SessionStorage;
using CommonLib.Source.Common.Extensions;
//using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;

namespace CommonLib.Web.Source.Common.Extensions
{
    public static class SessionStorageExtensions
    {
        public static async Task<Guid> GetSessionIdAsync(this ISessionStorageService sessionStorage)
        {
            var sessionId = await sessionStorage.ParseSessionIdAsync();
            return sessionId == Guid.Empty ? throw new NullReferenceException("\"SessionId\" not present") : sessionId;
        }

        public static async Task<Guid> GetSessionIdOrEmptyAsync(this ISessionStorageService sessionStorage)
        {
            return await sessionStorage.ParseSessionIdAsync();
        }

        public static async Task<Guid> GetOrCreateSessionIdAsync(this ISessionStorageService sessionStorage)
        {
            var sessionId = await sessionStorage.ParseSessionIdAsync();
            if (sessionId == Guid.Empty)
                sessionId = Guid.NewGuid();

            await sessionStorage.SetItemAsStringAsync("SessionId", sessionId.ToString());
            return sessionId;
        }

        private static async Task<Guid> ParseSessionIdAsync(this ISessionStorageService sessionStorage)
        {
            var storageProvider = sessionStorage.GetField<object>("_storageProvider");
            var jsRuntime = storageProvider.GetField<IJSRuntime>("_jSRuntime");
            var isInitialized = jsRuntime.GetProperty<bool>("IsInitialized");
            if (!isInitialized)
                return Guid.Empty;

            var strSessId = await sessionStorage.GetItemAsStringAsync("SessionId");
            var isSessionIdParsable = Guid.TryParse(strSessId, out var sessionId);
            return isSessionIdParsable ? sessionId : Guid.Empty;
        }


        //public static async Task<Guid> GetSessionIdAsync(this ProtectedBrowserStorage sessionStorage)
        //{
        //    var sessionId = await sessionStorage.ParseSessionIdAsync();
        //    return sessionId == Guid.Empty ? throw new NullReferenceException("\"SessionId\" not present") : sessionId;
        //}

        //public static async Task<Guid> GetSessionIdOrEmptyAsync(this ProtectedBrowserStorage sessionStorage)
        //{
        //    return await sessionStorage.ParseSessionIdAsync();
        //}

        //public static async Task<Guid> GetOrCreateSessionIdAsync(this ProtectedBrowserStorage sessionStorage)
        //{
        //    var sessionId = await sessionStorage.ParseSessionIdAsync();
        //    if (sessionId == Guid.Empty)
        //        sessionId = Guid.NewGuid();

        //    await sessionStorage.SetAsync("SessionId", sessionId);
        //    return sessionId;
        //}

        //private static async Task<Guid> ParseSessionIdAsync(this ProtectedBrowserStorage sessionStorage)
        //{
        //    var sessionIdResult = await sessionStorage.GetAsync<Guid>("SessionId");
        //    return sessionIdResult.Success ? sessionIdResult.Value : Guid.Empty;
        //}
    }
}
