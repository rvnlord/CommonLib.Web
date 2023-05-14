using SimpleInjector;
using System.Reflection;
using System.Text.Encodings.Web;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Utils;
using CommonLib.Web.Source.Services;
using CommonLib.Web.Source.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using SimpleInjector.Lifestyles;
using LogLevel = NLog.LogLevel;

namespace CommonLib.Web.Server.Source.Common.Utils
{
    public static class WebServerUtils
    {
        private static IHttpContextAccessor _httpContextAccessor = default!;
        private static IActionContextAccessor _actionContextAccessor = default!;

        public static string BackendUrl { get; private set; } = default!;
        public static WebApplicationBuilder HostBuilder { get; set; } = default!;
        public static IConfiguration Configuration => HostBuilder.Configuration;
        public static HttpClient HttpClient => GetService<HttpClient>();
        public static HtmlEncoder HtmlEncoder => GetService<HtmlEncoder>();
        public static UrlEncoder UrlEncoder => GetService<UrlEncoder>();
        public static Container ServiceProvider { get; set; } = default!;
        public static IServiceProvider OriginalServiceProvider { get; set; } = default!;
        public static IServiceCollection Services => HostBuilder.Services;
        public static IWebHostEnvironment HostEnvironment => HostBuilder.Environment;
        public static HttpContext HttpContext => _httpContextAccessor.HttpContext ?? default!;

        public static IJSRuntime JsRuntime { get; private set; } = default!;
        
        public static void Configure(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;
        public static void Configure(IActionContextAccessor actionContextAccessor) => _actionContextAccessor = actionContextAccessor;
        public static void Configure(IJSRuntime js) => JsRuntime = js;
        public static void Configure(Container serviceProvider) => ServiceProvider = serviceProvider;
        public static void Configure(IServiceProvider serviceProvider) => OriginalServiceProvider = serviceProvider;

        public static string GetRelativeVirtualPath() => GetAbsoluteVirtualPath().AfterFirst("://").AfterFirst("/");
        public static string GetRelativeVirtualPath(string to) => GetRelativeVirtualPath().TrimEnd('~', '/') + '/' + to.TrimStart('~', '/');
        public static string GetAbsoluteVirtualPath() => ConfigUtils.FrontendBaseUrl;
        //{
        //    var (nav, navScope) = GetScopedService<NavigationManager>();
        //    var baseUrl = nav.BaseUri;
        //    navScope.DisposeScopeAsync();
        //    return baseUrl;
        //}

        public static string GetAbsoluteVirtualPath(string to) => GetAbsoluteVirtualPath().TrimEnd('~', '/') + '/' + to.TrimStart('~', '/');

        public static string GetAbsolutePhysicalPath() => HostEnvironment.ContentRootPath;

        public static string GetAbsolutePhysicalProjectPath<T>() => FileUtils.GetProjectPath<T>();

        public static string GetAbsolutePhysicalProjectDir<T>() => FileUtils.GetProjectDir<T>();

        public static string GetAbsolutePhysicalContentPath(RunType? runType = null)
        {
            runType ??= FileUtils.GetRunType();
            var executingAssemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var contentPath = runType switch
            {
                RunType.AspNet => GetAbsolutePhysicalPath(),
                RunType.AspNetWithRawElectron => GetAbsolutePhysicalPath().BeforeFirstOrWholeIgnoreCase(@"\obj\Host\bin"),
                RunType.AspNetWithBuiltElectron => executingAssemblyDir.BeforeFirstOrWholeIgnoreCase(@"\bin"),
                RunType.AspNetWithPortableElectron => executingAssemblyDir.BeforeFirstOrWholeIgnoreCase(@"\bin"),
                _ => throw new Exception("Invalid Run Type"),
            };
            return contentPath;
        }

        public static T GetService<T>() where T : class => (T) GetService(typeof(T));

        public static object GetService(Type serviceType)
        {
            var lifestyle = ServiceProvider.GetRegistration(serviceType)?.Lifestyle;
            if (lifestyle == null)
                throw new NullReferenceException($"\"{serviceType.Name}\" has no Lifestyle defined");
            if (lifestyle.Name.In("Scoped", "Async Scoped", "Transient"))
                throw new ArgumentException($"{serviceType} Service can't be \"{lifestyle.Name}\", for \"Scoped\" or \"Transient\" Service use \"GetScopedService<T>()\" instead");
            return ServiceProvider.GetInstance(serviceType);
        }

        public static (T, Scope) GetScopedService<T>() where T : class
        {
            var (service, scope) = GetScopedService(typeof(T));
            return ((T)service, scope);
        }

        public static (object, Scope) GetScopedService(Type serviceType)
        {
            var lifestyle = ServiceProvider.GetRegistration(serviceType)?.Lifestyle;
            if (lifestyle == null)
                throw new NullReferenceException($"\"{serviceType.Name}\" has no Lifestyle defined");
            if (!lifestyle.Name.In("Scoped", "Async Scoped", "Transient"))
                throw new ArgumentException($"{serviceType} Service is \"{lifestyle.Name}\" instead of \"Scoper\" or \"Transient\"");
            var scope = AsyncScopedLifestyle.BeginScope(ServiceProvider);
            return (ServiceProvider.GetInstance(serviceType), scope);
        }

        public static DbContextOptions<TContext> GetMSSQLDbContextOptions<TContext>() where TContext : Microsoft.EntityFrameworkCore.DbContext
        {
            return new DbContextOptionsBuilder<TContext>().UseSqlServer(Configuration.GetConnectionString("DBCS")).Options;
        }

        public static DbContextOptions<TContext> GetSQLiteDbContextOptions<TContext>(string csConfigName = "DBCS") where TContext : Microsoft.EntityFrameworkCore.DbContext
        {
            return new DbContextOptionsBuilder<TContext>().UseSqlite(Configuration.GetConnectionString(csConfigName)).Options;
        }

        public static void ConfigureBackendUrl(Action postAction)
        {
            Task.Run(async () =>
            {
                await ConfigureBackendUrlAsync();
                postAction();
            });
        }

        public static async Task ConfigureBackendUrlAsync()
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (BackendUrl is not null)
                return;

            var backendUrls = Configuration.GetSection("BackendUrls").Get<string[]>() ?? Array.Empty<string>();
            foreach (var backendUrl in backendUrls)
            {
                var backendInfoClient = new BackendInfoClient(new HttpClient { BaseAddress = new Uri(backendUrl) }, null);
                var isPingSuccessful = !(await backendInfoClient.PingAsync()).IsError;
                if (isPingSuccessful)
                {
                    BackendUrl = backendUrl;
                    break;
                }
                if (backendUrl.EqualsInvariant(backendUrls.Last()))
                    Logger.For(typeof(WebServerUtils)).Log(LogLevel.Warn, "\"BackendUrls\" doesn't contain valid path to the Backend");
            }
        }

        public static void UseGlobalHostBuilder(WebApplicationBuilder builder)
        {
            HostBuilder = builder;
            WebUtils.ServerHostBuilder = builder;
        }

        public static void ConfigureUrls(string frontendBaseUrl) => Task.Run(async () => await ConfigureUrlsAsync(frontendBaseUrl));

        public static async Task ConfigureUrlsAsync(string frontendBaseUrl)
        {
            ConfigUtils.BackendBaseUrl = BackendUrl; // this is required for API endpoints (even from this function) to work properly
            ConfigUtils.FrontendBaseUrl = frontendBaseUrl;

            if (BackendUrl.IsNullOrWhiteSpace())
            {
                Logger.For(typeof(WebServerUtils)).Log(LogLevel.Warn, "\"BackendUrl\" is not set");
                return;
            }

            var (backendInfo, backendInfoScope) = GetScopedService<IBackendInfoClient>();
            
            await backendInfo.SetBackendBaseUrlAsync(ConfigUtils.BackendBaseUrl);
            await backendInfo.SetFrontendBaseUrlAsync(ConfigUtils.FrontendBaseUrl);

            var dbcsResp = await backendInfo.GetBackendDBCSAsync();
            ConfigUtils.BackendDBCS = dbcsResp.IsError ? "No Backend Db Connection Retrieved" : dbcsResp.Result;
            ConfigUtils.FrontendDBCS = HostBuilder.Configuration.GetSection("ConnectionStrings").GetSection("ClientDb").Value;

            await backendInfoScope.DisposeScopeAsync();
        }
    }
}
