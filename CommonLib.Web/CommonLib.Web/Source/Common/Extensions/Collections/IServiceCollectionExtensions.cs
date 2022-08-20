using System.IO;
using System.Linq;
using System.Reflection;
using CommonLib.Web.Source.Services;
using CommonLib.Web.Source.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CommonLib.Web.Source.Common.Extensions.Collections
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddJQuery(this IServiceCollection services)
        {
            services.AddScoped<IJQuery, JQuery>();
            return services;
        }

        public static IServiceCollection AddAnimeJs(this IServiceCollection services)
        {
            services.AddScoped<IAnimeJs, AnimeJs>();
            return services;
        }

        public static IServiceCollection AddMyJsRuntime(this IServiceCollection services)
        {
            services.AddScoped<IMyJsRuntime, MyJsRuntime>();
            return services;
        }

        public static IServiceCollection AddRequestScopedCache(this IServiceCollection services)
        {
            services.AddScoped<IRequestScopedCacheService, RequestScopedCacheService>();
            return services;
        }

        public static T GetService<T>(this IServiceCollection services)
        {
            return (T) (object) services.Single(s => s.ServiceType is T).ServiceType;
        }
    }
}
