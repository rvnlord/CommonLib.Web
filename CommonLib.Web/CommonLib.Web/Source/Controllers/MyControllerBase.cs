using System;
using System.Linq;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Models;
using CommonLib.Source.Models.Interfaces;
using CommonLib.Web.Source.Models;
using CommonLib.Web.Source.Models.Interfaces;
using CommonLib.Web.Source.ViewModels.Account;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace CommonLib.Web.Source.Controllers
{
    public abstract class MyControllerBase : ControllerBase
    {
        protected readonly IApiResponse _defaultInvalidResponse;

        protected MyControllerBase()
        {
            _defaultInvalidResponse = new ApiResponse(StatusCodeType.Status400BadRequest, "Invalid model", 
                ModelState.Values.SelectMany(v => v.Errors).ToLookup(e => ModelState.Single(s => s.Value.Errors.Contains(e)).Key, e => e.ErrorMessage));
        }

        protected async Task<JToken> EnsureResponseAsync<T>(Func<Task<ApiResponse<T>>> actionAsync)
        {
            try
            {
                return (ModelState.IsValid ? await actionAsync() : _defaultInvalidResponse).ToJToken();
            }
            catch (Exception ex)
            {
                return new ApiResponse(StatusCodeType.Status500InternalServerError, $"API Server threw {ex.GetType().Name}: {ex.Message}", null, null, ex).ToJToken();
            }
        }

        protected async Task<JToken> EnsureVoidResponseAsync(Func<Task<IApiResponse>> actionAsync)
        {
            try
            {
                return (ModelState.IsValid ? await actionAsync() : _defaultInvalidResponse).ToJToken();
            }
            catch (Exception ex)
            {
                return new ApiResponse(StatusCodeType.Status500InternalServerError, $"API Server threw {ex.GetType().Name}: {ex.Message}", null, null, ex).ToJToken();
            }
        }
    }
}
