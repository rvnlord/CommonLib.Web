using System;
using System.Collections.Generic;
using CommonLib.Web.Source.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CommonLib.Web.Source.Services
{
    public class HttpContextServiceProviderProxy : IServiceProviderProxy
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public HttpContextServiceProviderProxy(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public T GetService<T>()
        {
            return (_contextAccessor.HttpContext ?? throw new NullReferenceException(nameof(_contextAccessor.HttpContext))).RequestServices.GetService<T>();
        }

        public IEnumerable<T> GetServices<T>()
        {
            return (_contextAccessor.HttpContext ?? throw new NullReferenceException(nameof(_contextAccessor.HttpContext))).RequestServices.GetServices<T>();
        }

        public object GetService(Type type)
        {
            return (_contextAccessor.HttpContext ?? throw new NullReferenceException(nameof(_contextAccessor.HttpContext))).RequestServices.GetService(type);
        }

        public IEnumerable<object> GetServices(Type type)
        {
            return (_contextAccessor.HttpContext ?? throw new NullReferenceException(nameof(_contextAccessor.HttpContext))).RequestServices.GetServices(type);
        }
    }
}
