using System.Diagnostics;
using System.Text.Encodings.Web;
using CommonLib.Web.Server.Source.Common.Utils;
using CommonLib.Web.Source.Common.Utils;
using CommonLib.Web.Server.Source.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using NuGet.Packaging.Licenses;

namespace CommonLib.Web.Server.Source.Services
{
    public class RenderPartial : IRenderPartial
    {
        private readonly IRazorViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IRazorPageActivator _activator;

        public RenderPartial(
            IRazorViewEngine viewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider,
            IWebHostEnvironment env,
            IHttpContextAccessor httpContextAccessor,
            IRazorPageActivator activator)
        {
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
            _env = env;
            _httpContextAccessor = httpContextAccessor;
            _activator = activator;
        }

        public async Task<string> RenderPartialAsync(string partialName) 
        {
            var actionContext = GetActionContext();
            var partial = FindView(actionContext, partialName);
            await using var output = new StringWriter();
            var viewContext = new ViewContext(
                actionContext, partial,
                new ViewDataDictionary(
                    new EmptyModelMetadataProvider(),
                    new ModelStateDictionary()),
                new TempDataDictionary(
                    actionContext.HttpContext,
                    _tempDataProvider),
                output,
                new HtmlHelperOptions()
            );

            await partial.RenderAsync(viewContext).ConfigureAwait(false); // this will throw if we define `DefaultHttpContext` instead of injecting one
            return output.ToString();
        }

        public async Task<string> RenderPartialAsync<TModel>(string partialName, TModel model) 
        {
            var actionContext = GetActionContext();
            var partial = FindView(actionContext, partialName);
            await using var output = new StringWriter();
            var viewContext = new ViewContext(actionContext, partial, 
                new ViewDataDictionary<TModel>(new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = model },
                new TempDataDictionary(actionContext.HttpContext, _tempDataProvider), output, new HtmlHelperOptions());
            await partial.RenderAsync(viewContext).ConfigureAwait(false);
            return output.ToString();
        }

        private IView FindView(ActionContext actionContext, string partialName)
        {
            var getPartialResult = _viewEngine.GetView(WebServerUtils.GetAbsoluteVirtualPath(), partialName, false);
            if (getPartialResult.Success)
                return getPartialResult.View;

            var findPartialResult = _viewEngine.FindView(actionContext, partialName, false);
            if (findPartialResult.Success)
                return findPartialResult.View;

            var searchedLocations = getPartialResult.SearchedLocations.Concat(findPartialResult.SearchedLocations);
            var errorMessage = string.Join(
                Environment.NewLine,
                new[] { $"Unable to find partial '{partialName}'. The following locations were searched:" }.Concat(searchedLocations));
            throw new InvalidOperationException(errorMessage);
        }

        private ActionContext GetActionContext()
        {
            if (_httpContextAccessor.HttpContext == null)
                throw new NullReferenceException("HttpContext is null");
            return new ActionContext(_httpContextAccessor.HttpContext, new RouteData(), new ActionDescriptor());
        }

        public async Task<string> RenderPageAsync(string pageName)
        {
            var actionContext = GetActionContext();
            var page = (Page) FindPage(actionContext, pageName);
            var view = new RazorView(_viewEngine, _activator, new List<IRazorPage>(), page, HtmlEncoder.Default, new DiagnosticListener("ViewRenderService"));
            await using var output = new StringWriter();
            var viewContext = new ViewContext(actionContext, view,
                new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
                output, new HtmlHelperOptions()
            );
            page.PageContext = new PageContext { ViewData = viewContext.ViewData };
            page.ViewContext = viewContext;
            _activator.Activate(page, viewContext);
            await page.ExecuteAsync();
            return output.ToString();
        }

        public async Task<string> RenderPageAsync<TModel>(string pageName, TModel model)
        {
            var actionContext = GetActionContext();
            var page = (Page) FindPage(actionContext, pageName);
            var view = new RazorView(_viewEngine, _activator, new List<IRazorPage>(), page, HtmlEncoder.Default, new DiagnosticListener("ViewRenderService"));
            await using var output = new StringWriter();
            var viewContext = new ViewContext(actionContext, view,
                new ViewDataDictionary<TModel>(new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = model },
                new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
                output, new HtmlHelperOptions()
            );
            page.PageContext = new PageContext { ViewData = viewContext.ViewData };
            page.ViewContext = viewContext;
            _activator.Activate(page, viewContext);
            await page.ExecuteAsync();
            return output.ToString();
        }

        private IRazorPage FindPage(ActionContext actionContext, string pageName)
        {
            var getPageResult = _viewEngine.GetPage(WebServerUtils.GetAbsoluteVirtualPath(), pageName);
            if (getPageResult.Page != null)
                return getPageResult.Page;

            var findPageResult = _viewEngine.FindPage(actionContext, pageName);
            if (findPageResult.Page != null)
                return findPageResult.Page;

            var searchedLocations = (getPageResult.SearchedLocations ?? Enumerable.Empty<string>()).Concat(findPageResult.SearchedLocations ?? Enumerable.Empty<string>());
            var errorMessage = string.Join(
                Environment.NewLine,
                new[] { $"Unable to find partial '{pageName}'. The following locations were searched:" }.Concat(searchedLocations));
            throw new InvalidOperationException(errorMessage);
        }
    }
}