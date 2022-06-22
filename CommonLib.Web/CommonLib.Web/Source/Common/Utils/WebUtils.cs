using System;
using SimpleInjector;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils;
using CommonLib.Web.Source.Services;
using CommonLib.Web.Source.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SimpleInjector.Lifestyles;

namespace CommonLib.Web.Source.Common.Utils
{
    public static class WebUtils
    {
        public static string BackendUrl { get; private set; }
        public static WebAssemblyHostBuilder HostBuilder { get; set; }
        public static IConfiguration Configuration => HostBuilder.Configuration;
        public static HttpClient HttpClient => GetService<HttpClient>();
        public static HtmlEncoder HtmlEncoder => GetService<HtmlEncoder>();
        public static UrlEncoder UrlEncoder => GetService<UrlEncoder>();
        public static Container ServiceProvider { get; set; }
        
        public static IJSRuntime JsRuntime { get; private set; }
        
        public static void Configure(IJSRuntime js) => JsRuntime = js;
        public static void Configure(Container serviceProvider) => ServiceProvider = serviceProvider;

        public static void ConfigureTendermint()
        {
            var tendermint = Process.Start(new ProcessStartInfo
            {
                FileName = @"Tendermint\tendermint.exe",
                Arguments = "init validator --home=Tendermint"
            });
            tendermint?.WaitForExit();

            Process.Start(new ProcessStartInfo
            {
                FileName = @"Tendermint\tendermint.exe",
                Arguments = @"node --abci grpc --home=Tendermint" // @"node --abci grpc --proxy_app http://127.0.0.1:5020 --home=Tendermint --log_level debug"
            });
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
            if (BackendUrl != null)
                return;

            var backendUrls = Configuration.GetSection("BackendUrls").Get<string[]>();
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
                    throw new ArgumentException("\"BackendUrls\" doesn't contain valid path to the Backend");
            }
        }

        public static void UseGlobalHostBuilder(WebAssemblyHostBuilder builder)
        {
            HostBuilder = builder;
        }

        public static void ConfigureUrls() => Task.Run(async () => await ConfigureUrlsAsync());

        public static async Task ConfigureUrlsAsync()
        {
            var (backendInfo, backendInfoScope) = GetScopedService<IBackendInfoClient>();

            ConfigUtils.BackendBaseUrl = BackendUrl; // this is required for API endpoints (even from this function) to work properly
            await backendInfo.SetBackendBaseUrlAsync(ConfigUtils.BackendBaseUrl);

            ConfigUtils.FrontendBaseUrl = HostBuilder.HostEnvironment.BaseAddress;
            await backendInfo.SetFrontendBaseUrlAsync(HostBuilder.HostEnvironment.BaseAddress);
            
            var dbcsResp = await backendInfo.GetBackendDBCSAsync();
            ConfigUtils.BackendDBCS = dbcsResp.IsError ? "No Backend Db Connection Retrieved" : dbcsResp.Result;

            ConfigUtils.FrontendDBCS = HostBuilder.Configuration.GetSection("ConnectionStrings").GetSection("ClientDb").Value;
            
            await backendInfoScope.DisposeScopeAsync();
        }
    }
}
