using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Blazored.SessionStorage;
using CommonLib.Web.Source.Models;
using CommonLib.Web.Source.Models.Interfaces;
using CommonLib.Web.Source.Services.Account.Interfaces;
using CommonLib.Web.Source.ViewModels.Account;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Models;
using CommonLib.Web.Source.Common.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.JSInterop;
using Newtonsoft.Json.Linq;
using CommonLib.Web.Source.Common.Extensions;
using Z.Expressions;

namespace CommonLib.Web.Source.Services.Account
{
    public class AccountClient : IAccountClient
    {
        private HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;
        private readonly ILocalStorageService _localStorage;
        private readonly ISessionStorageService _sessionStorage;

        public HttpClient HttpClient
        {
            get
            {
                if (_httpClient.BaseAddress == null && ConfigUtils.BackendBaseUrl != null)
                    _httpClient.BaseAddress = new Uri(ConfigUtils.BackendBaseUrl);
                return _httpClient;
            }
            set =>  _httpClient = value;
        }

        public AccountClient(HttpClient httpClient, IJSRuntime jsRuntime, ILocalStorageService localStorage, ISessionStorageService sessionStorage)
        {
            HttpClient = httpClient;
            _jsRuntime = jsRuntime;
            _localStorage = localStorage;
            _sessionStorage = sessionStorage;
        }

        public async Task<ApiResponse<FindUserVM>> FindUserByNameAsync(string email)
        {
            return await HttpClient.PostJTokenAsync<ApiResponse<FindUserVM>>("api/account/finduserbyname", email);
        }

        public async Task<ApiResponse<FindUserVM>> FindUserByEmailAsync(string email)
        {
            return await HttpClient.PostJTokenAsync<ApiResponse<FindUserVM>>("api/account/finduserbyemail", email);
        }

        public async Task<ApiResponse<bool>> CheckUserManagerComplianceAsync(string userPropertyName, string userPropertyDisplayName, string userPropertyValue)
        {
            return await HttpClient.PostJTokenAsync<ApiResponse<bool>>("api/account/checkusermanagercompliance", new JObject
            {
                ["UserPropertyName"] = userPropertyName, 
                ["UserPropertyDisplayName"] = userPropertyDisplayName, 
                ["UserPropertyValue"] = userPropertyValue
            });
        }

        public async Task<ApiResponse<AuthenticateUserVM>> GetAuthenticatedUserAsync()
        {
            var isInitialized = _jsRuntime.GetProperty<bool>("IsInitialized");
            var cookieTIcket = isInitialized ? await _jsRuntime.InvokeAndCatchCancellationAsync<string>("Cookies.get", "Ticket") : null;
            var localStorageTicket = isInitialized ? await _localStorage.GetItemAsStringAsync("Ticket") : null;
            var sessionId = isInitialized ? await _sessionStorage.GetSessionIdOrEmptyAsync() : Guid.Empty;
            var userToAuthenticate = new AuthenticateUserVM
            {
                Ticket = cookieTIcket ?? localStorageTicket, 
                AuthenticationStatus = AuthStatus.NotChecked,
                SessionId = sessionId
            };
            return await HttpClient.PostJTokenAsync<ApiResponse<AuthenticateUserVM>>("api/account/authenticateuser", userToAuthenticate);
        }

        public async Task<ApiResponse<RegisterUserVM>> RegisterAsync(RegisterUserVM userToRegister)
        {
            var registerResp = await HttpClient.PostJTokenAsync<ApiResponse<RegisterUserVM>>("api/account/register", userToRegister);
            if (registerResp.IsError)
                return registerResp;

            if (registerResp.Result.Ticket != null)
            {
                await _jsRuntime.InvokeVoidAsync("Cookies.set", "Ticket", registerResp.Result.Ticket);
                await _localStorage.RemoveItemAsync("Ticket");
            }

            return registerResp;
        }

        public async Task<ApiResponse<ConfirmUserVM>> ConfirmEmailAsync(ConfirmUserVM userToConfirmEmail)
        {
            return await HttpClient.PostJTokenAsync<ApiResponse<ConfirmUserVM>>("api/account/confirmemail", userToConfirmEmail);
        }

        public async Task<ApiResponse<FindUserVM>> FindUserByConfirmationCodeAsync(string activationCode)
        {
            return await HttpClient.PostJTokenAsync<ApiResponse<FindUserVM>>("api/account/finduserbyconfirmationcode", activationCode);
        }

        public async Task<ApiResponse<ResendConfirmationEmailUserVM>> ResendConfirmationEmailAsync(ResendConfirmationEmailUserVM resendConfirmationEmailUser)
        {
            return await HttpClient.PostJTokenAsync<ApiResponse<ResendConfirmationEmailUserVM>>("api/account/resendconfirmationemail", resendConfirmationEmailUser);
        }

        public async Task<ApiResponse<LoginUserVM>> LoginAsync(LoginUserVM userToLogin)
        {
            var loginResponse = await HttpClient.PostJTokenAsync<ApiResponse<LoginUserVM>>("api/account/login", userToLogin);
            if (loginResponse.IsError)
                return loginResponse;

            var loggedUser = loginResponse.Result;
            if (loggedUser.RememberMe)
            {
                await _jsRuntime.InvokeVoidAsync("Cookies.set", "Ticket", loggedUser.Ticket, new { expires = 365 * 24 * 60 * 60 });
                await _localStorage.SetItemAsync("Ticket", loggedUser.Ticket);
            }
            else
            {
                await _jsRuntime.InvokeVoidAsync("Cookies.set", "Ticket", loggedUser.Ticket);
                await _localStorage.RemoveItemAsync("Ticket");
            }

            return loginResponse;
        }

        public async Task<ApiResponse<LoginUserVM>> ExternalLoginAuthorizeAsync(LoginUserVM userToExternalLogin)
        {
            var loginResponse = await _httpClient.PostJTokenAsync<ApiResponse<LoginUserVM>>("api/account/externalloginauthorize", userToExternalLogin);
            if (loginResponse.IsError)
                return loginResponse;

            var loggedUser = loginResponse.Result;
            if (loggedUser.RememberMe)
            {
                await _jsRuntime.InvokeVoidAsync("Cookies.set", "Ticket", loggedUser.Ticket, new { expires = 365 * 24 * 60 * 60 });
                await _localStorage.SetItemAsync("Ticket", loginResponse.Result.Ticket);
            }
            else
            {
                await _jsRuntime.InvokeVoidAsync("Cookies.set", "Ticket", loggedUser.Ticket);
                await _localStorage.RemoveItemAsync("Ticket");
            }

            return loginResponse;
        }

        public async Task<ApiResponse<IList<AuthenticationScheme>>> GetExternalAuthenticationSchemesAsync()
        {
            return await HttpClient.PostJTokenAsync<ApiResponse<IList<AuthenticationScheme>>>("api/account/getexternalauthenticationschemes"); // or IApiResponse .ToGeneric(jt => jt.To<IList<AuthenticationScheme>>());
        }

        public async Task<ApiResponse<AuthenticateUserVM>> LogoutAsync()
        {
            var authUser = (await GetAuthenticatedUserAsync())?.Result;
            var logoutResponse = await HttpClient.PostJTokenAsync<ApiResponse<AuthenticateUserVM>>("api/account/logout", authUser);

            if (logoutResponse.IsError)
                return logoutResponse;

            await _jsRuntime.InvokeVoidAsync("Cookies.expire", "Ticket");
            await _localStorage.RemoveItemAsync("Ticket");

            return logoutResponse;
        }

        public async Task<ApiResponse<ForgotPasswordUserVM>> ForgotPasswordAsync(ForgotPasswordUserVM forgotPasswordUser)
        {
            return await HttpClient.PostJTokenAsync<ApiResponse<ForgotPasswordUserVM>>("api/account/forgotpassword", forgotPasswordUser);
        }

        public async Task<ApiResponse<ResetPasswordUserVM>> ResetPasswordAsync(ResetPasswordUserVM resetPasswordUserVM)
        {
            return await HttpClient.PostJTokenAsync<ApiResponse<ResetPasswordUserVM>>("api/account/resetpassword", resetPasswordUserVM);
        }

        public async Task<ApiResponse<bool>> CheckUserResetPasswordCodeAsync(CheckResetPasswordCodeUserVM userToCheckResetPasswordCode)
        {
            return await HttpClient.PostJTokenAsync<ApiResponse<bool>>($"Api/Account/{nameof(CheckUserResetPasswordCodeAsync).Before("Async")}", userToCheckResetPasswordCode);
        }

        public async Task<ApiResponse<bool>> CheckUserPasswordAsync(CheckPasswordUserVM userToCheckPassword)
        {
            return await HttpClient.PostJTokenAsync<ApiResponse<bool>>($"Api/Account/{nameof(CheckUserPasswordAsync).Before("Async")}", userToCheckPassword);
        }

        public async Task<ApiResponse<FindUserVM>> FindUserByIdAsync(Guid id)
        {
            return await HttpClient.PostJTokenAsync<ApiResponse<FindUserVM>>("api/account/finduserbyid", id);
        }

        public async Task<ApiResponse<EditUserVM>> EditAsync(EditUserVM editUser)
        {
            var authUser = (await GetAuthenticatedUserAsync())?.Result;
            var editResp = await HttpClient.PostJTokenAsync<ApiResponse<EditUserVM>>("api/account/edit", new
            {
                AuthenticatedUser = authUser, 
                UserToEdit = editUser
            });

            if (editResp.IsError)
                return editResp;

            if (authUser?.RememberMe == true)
            {
                await _jsRuntime.InvokeVoidAsync("Cookies.set", "Ticket", editResp.Result.Ticket, new { expires = 365 * 24 * 60 * 60 });
                await _localStorage.SetItemAsync("Ticket",  editResp.Result.Ticket);
            }
            else
            {
                await _jsRuntime.InvokeVoidAsync("Cookies.set", "Ticket", editResp.Result.Ticket);
                await _localStorage.RemoveItemAsync("Ticket");
            }

            return editResp;
        }

    }
}
