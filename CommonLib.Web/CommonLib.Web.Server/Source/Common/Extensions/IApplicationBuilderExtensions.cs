using System.Diagnostics.CodeAnalysis;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Server.Source.Common.Utils;
using CommonLib.Web.Source;
using CommonLib.Web.Source.Common.Utils;
using CommonLib.Web.Source.Services;
using CommonLib.Web.Source.Services.Interfaces;
using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using SimpleInjector;
using LogLevel = NLog.LogLevel;

namespace CommonLib.Web.Server.Source.Common.Extensions
{
    public static class IApplicationBuilderExtensions
    {
        public static void UseGlobalHttpContext(this IApplicationBuilder app)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            WebServerUtils.Configure(app.ApplicationServices.GetRequiredService<IHttpContextAccessor>());
        }

        public static void UseGlobalActionContext(this IApplicationBuilder app)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            WebServerUtils.Configure(app.ApplicationServices.GetRequiredService<IActionContextAccessor>());
        }

        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public static void UseGlobalJsRuntime(this IApplicationBuilder app, IJSRuntime js)
        {
            WebServerUtils.Configure(js);
        }

        public static void UseGlobalServiceProvider(this IApplicationBuilder app, Container container)
        {
            WebServerUtils.Configure(container);
        }

        public static void UseGlobalServiceProvider(this IApplicationBuilder app, IServiceProvider serviceProvider)
        {
            WebServerUtils.Configure(serviceProvider);
        }

        public static void UseTendermint(this IApplicationBuilder app)
        {
            WebUtils.ConfigureTendermint();
        }

        public static IApplicationBuilder UseGlobalLogger(this IApplicationBuilder app)
        {
            AspNetLibConfig.RunType = FileUtils.GetRunType();
            Logger.LogPath = FileUtils.GetLogPath(AspNetLibConfig.RunType);
            return app;
        }

        public static IApplicationBuilder UseServiceLocator(this IApplicationBuilder app, IServiceProvider sp)
        {
            ServiceLocator.Initialize(sp.GetService<IServiceProviderProxy>());
            return app;
        }

        public static IApplicationBuilder LicenseZUtils(this IApplicationBuilder app)
        {
            ZUtils.EnsureLicensed();
            return app;
        }

        public static IApplicationBuilder LogLoggerPath<TStartup>(this IApplicationBuilder app)
        {
            Logger.For<TStartup>().Info($@"Log Path: {Logger.LogPath}");
            return app;
        }

        public static IApplicationBuilder RunTestLogs<TStartup>(this IApplicationBuilder app)
        {
            var logger = Logger.For<TStartup>().Log(LogLevel.Info, $@"Log Path: {Logger.LogPath}"); // it shouldn't change LogPath defined in global logger
            logger.Log(LogLevel.Info, $@"Entry Assembly Location: {FileUtils.GetEntryAssemblyDir()}");
            logger.Log(LogLevel.Info, $@"Is Electron? Raw: {AspNetLibConfig.RunType == RunType.AspNetWithRawElectron} || Full: {AspNetLibConfig.RunType == RunType.AspNetWithBuiltElectron} || Portable: {AspNetLibConfig.RunType == RunType.AspNetWithPortableElectron}");
            logger.Log(LogLevel.Info, $@"Content Path: {WebServerUtils.GetAbsolutePhysicalContentPath(AspNetLibConfig.RunType)}");
            return app;
        }

        public static IApplicationBuilder OpenElectronWindowIfNecessary(this IApplicationBuilder app)
        {
            AspNetLibConfig.IsElectron = AspNetLibConfig.RunType != RunType.AspNet;
            if (AspNetLibConfig.IsElectron)
            {
                var options = new BrowserWindowOptions
                {
                    Show = true,
                    Frame = false,
                    HasShadow = true,
                    Icon = $@"{WebServerUtils.GetAbsolutePhysicalContentPath(AspNetLibConfig.RunType)}\Content\Icons\Icon.ico",
                    Height = 720,
                    Width = 1240
                };

                Task.Run(async () => await Electron.WindowManager.CreateWindowAsync(options).ConfigureAwait(false));
                //Electron.Dialog.ShowErrorBox("Log", $@"Log Path: {LoggerUtils.LogPath}");
                //Electron.Dialog.ShowErrorBox("Log", $@"Is Electron? Raw: {RunType == ElectronRunType.Raw} || Full: {RunType == ElectronRunType.Built} || Portable: {RunType == ElectronRunType.Portable}");
            }
            return app;
        }
    }
}
