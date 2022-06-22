using System.Reflection;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace CommonLib.Web.Server.Source.Common.Extensions.Collections
{
    public static class IServiceCollectionExtensions
    {
        
        public static IServiceCollection MergeBlazorPagesAndRazorViewsLocation(this IServiceCollection services)
        {
            services.Configure<RazorPagesOptions>(o => o.RootDirectory = "/Source/Pages");
            services.Configure<RazorViewEngineOptions>(o =>
            { //o.ViewLocationFormats.Clear(); // {2} is area, {1} is controller, {0} is the action   

                var cshtml = RazorViewEngine.ViewExtension;
                var razor = ".razor";
                var viewLocations = new[]
                {
                    "/Source/Pages/{1}/{0}" + cshtml,
                    "/Source/Views/{1}/{0}" + cshtml,
                    "/Source/Views/Shared/{0}" + cshtml,
                    "/Source/Views/Shared/Styles/{0}" + cshtml,
                    "/Source/Common/Pages/{1}/{0}" + cshtml,
                    "/Source/Common/Pages/Styles/{0}" + cshtml
                };

                o.ViewLocationFormats.Clear();
                o.PageViewLocationFormats.Clear();

                foreach (var location in viewLocations) // ViewLocationFormats in MVC, this in Blazor
                {
                    o.ViewLocationFormats.Add(location);
                    o.PageViewLocationFormats.Add(location);
                }
            });
            services.Configure<MvcRazorRuntimeCompilationOptions>(o =>
            {                
                var embeddedFileProvider = new EmbeddedFileProvider(
                    typeof(CommonLib.Web.Source.Common.Components.MyComponentBase).GetTypeInfo().Assembly,
                    "ViewComponentLibrary"
                );
                o.FileProviders.Add(embeddedFileProvider);
            });
            return services;
        }
    }
}
