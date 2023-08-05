using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Blazored.SessionStorage;
using CommonLib.Web.Source.Services.Account.Interfaces;
using CommonLib.Web.Source.ViewModels.Account;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.JSInterop;
using Newtonsoft.Json.Linq;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Services.Interfaces;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Source.ViewModels.Account;
using CommonLib.Web.Source.Controllers;
using CommonLib.Web.Source.Common.Pages.Admin;

namespace CommonLib.Web.Source.Services.Account
{
    public class AccountClient : IAccountClient
    {
        private HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;
        private readonly ILocalStorageService _localStorage;
        private readonly ISessionStorageService _sessionStorage;
        private readonly IMyJsRuntime _myJsRuntime;

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

        public AccountClient(HttpClient httpClient, IJSRuntime jsRuntime, ILocalStorageService localStorage, ISessionStorageService sessionStorage, IMyJsRuntime myJsRuntime)
        {
            HttpClient = httpClient;
            _jsRuntime = jsRuntime;
            _localStorage = localStorage;
            _sessionStorage = sessionStorage;
            _myJsRuntime = myJsRuntime;
        }

        public async Task<ApiResponse<FindUserVM>> FindUserByIdAsync(Guid id, bool includeEmailClaim = false)
        {
            return await HttpClient.PostJTokenAsync<ApiResponse<FindUserVM>>("api/account/finduserbyid", new JObject { ["Id"] = id, ["IncludeEmailClaim"] = includeEmailClaim });
        }

        public async Task<ApiResponse<FindUserVM>> FindUserByNameAsync(string email)
        {
            return await HttpClient.PostJTokenAsync<ApiResponse<FindUserVM>>("api/account/finduserbyname", email);
        }

        public async Task<ApiResponse<FindUserVM>> FindUserByEmailAsync(string email)
        {
            return await HttpClient.PostJTokenAsync<ApiResponse<FindUserVM>>("api/account/finduserbyemail", email);
        }

        public async Task<ApiResponse<FindUserVM>> FindUserByConfirmationCodeAsync(string activationCode)
        {
            return await HttpClient.PostJTokenAsync<ApiResponse<FindUserVM>>("api/account/finduserbyconfirmationcode", activationCode);
        }
        
        public async Task<ApiResponse<FindRoleVM>> FindRoleByNameAsync(string name)
        {
            return await HttpClient.PostJTokenAsync<ApiResponse<FindRoleVM>>("api/account/findrolebyname", name);
        }

        public async Task<ApiResponse<FindClaimVM>> FindClaimByNameAsync(string name)
        {
            return await HttpClient.PostJTokenAsync<ApiResponse<FindClaimVM>>("api/account/findclaimbyname", name);
        }

        public async Task<ApiResponse<FileData>> GetUserAvatarByNameAsync(string name)
        {
            return await HttpClient.PostJTokenAsync<ApiResponse<FileData>>($"api/account/{nameof(AccountApiController.GetUserAvatarByNameAsync).BeforeLast("Async")}", name);
        }

        public async Task<ApiResponse<FileDataList>> FindAvatarsInUseAsync(bool includeFileData)
        {
            return await HttpClient.PostJTokenAsync<ApiResponse<FileDataList>>($"api/account/{nameof(AccountApiController.FindAvatarsInUseAsync).BeforeLast("Async")}", includeFileData);
        }
        
        public async Task<ApiResponse<List<ExternalLoginVM>>> GetExternalLogins(string name)
        {
            var externalLoginsResp = await HttpClient.PostJTokenAsync<ApiResponse<List<ExternalLoginVM>>>($"api/account/{nameof(AccountApiController.GetExternalLoginsAsync)}", name);
            externalLoginsResp.Result ??= new List<ExternalLoginVM>();
            return externalLoginsResp;
        }

        public async Task<ApiResponse<List<WalletVM>>> GetWalletsAsync(string userName)
        {
            var walletsResp = await HttpClient.PostJTokenAsync<ApiResponse<List<WalletVM>>>($"api/account/{nameof(AccountApiController.GetWalletsAsync)}", userName);
            walletsResp.Result ??= new List<WalletVM>();
            return walletsResp;
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
            var isInitialized = await _jsRuntime.IsInitializedAsync();
            var cookieTIcket = isInitialized ? await _jsRuntime.InvokeAndCatchCancellationAsync<string>("Cookies.get", "Ticket") : null;
            var localStorageTicket = isInitialized ? await _localStorage.GetItemAsStringAsync("Ticket") : null;
            //var sessionId = isInitialized ? await _myJsRuntime.GetSessionIdOrEmptyAsync() : Guid.Empty;
            var userToAuthenticate = new AuthenticateUserVM
            {
                Ticket = cookieTIcket ?? localStorageTicket, 
                AuthenticationStatus = AuthStatus.NotChecked,
                //SessionId = sessionId
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
            var loginResponse = await HttpClient.PostJTokenAsync<ApiResponse<LoginUserVM>>("api/account/externalloginauthorize", userToExternalLogin);
            if (loginResponse.IsError)
                return loginResponse;

            var loggedUser = loginResponse.Result;

            if (loggedUser.Email is not null && !loggedUser.IsConfirmed)
                return loginResponse;

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

        public async Task<ApiResponse<LoginUserVM>> WalletLoginAsync(LoginUserVM userToWalletLogin)
        {
            var loginResponse = await HttpClient.PostJTokenAsync<ApiResponse<LoginUserVM>>("api/account/walletloginasync", userToWalletLogin);
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

        public async Task<ApiResponse<List<AuthenticationSchemeVM>>> GetExternalAuthenticationSchemesAsync()
        {
            return await HttpClient.PostJTokenAsync<ApiResponse<List<AuthenticationSchemeVM>>>("api/account/getexternalauthenticationschemes"); // or IApiResponse .ToGeneric(jt => jt.To<IList<AuthenticationScheme>>());
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

            if (editResp.Result.ShouldLogout)
            {
                await _jsRuntime.InvokeVoidAsync("Cookies.expire", "Ticket");
                await _localStorage.RemoveItemAsync("Ticket");
            }
            else
            {
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
            }
            
            return editResp;
        }

        public async Task<ApiResponse<EditUserVM>> ConnectExternalLoginAsync(EditUserVM editUser, LoginUserVM loginUser)
        {
            var authUser = (await GetAuthenticatedUserAsync())?.Result;
            var editResp = await HttpClient.PostJTokenAsync<ApiResponse<EditUserVM>>($"api/account/{nameof(AccountApiController.ConnectExternalLoginAsync)}", new
            {
                AuthenticatedUser = authUser, 
                UserToEdit = editUser,
                UserToLogin = loginUser
            });
            
            return editResp;
        }

        public async Task<ApiResponse<EditUserVM>> DisconnectExternalLoginAsync(EditUserVM editUser)
        {
            var authUser = (await GetAuthenticatedUserAsync())?.Result;
            var editResp = await HttpClient.PostJTokenAsync<ApiResponse<EditUserVM>>($"api/account/{nameof(AccountApiController.DisconnectExternalLoginAsync)}", new
            {
                AuthenticatedUser = authUser, 
                UserToEdit = editUser
            });
            
            return editResp;
        }

        public async Task<ApiResponse<EditUserVM>> ConnectWalletAsync(EditUserVM userToEdit)
        {
            var authUser = (await GetAuthenticatedUserAsync())?.Result;
            var editResp = await HttpClient.PostJTokenAsync<ApiResponse<EditUserVM>>($"api/account/{nameof(AccountApiController.ConnectWalletAsync)}", new
            {
                AuthenticatedUser = authUser, 
                UserToEdit = userToEdit
            });
            
            return editResp;
        }

        public async Task<ApiResponse<EditUserVM>> DisconnectWalletAsync(EditUserVM userToEdit)
        {
            var authUser = (await GetAuthenticatedUserAsync())?.Result;
            var editResp = await HttpClient.PostJTokenAsync<ApiResponse<EditUserVM>>($"api/account/{nameof(AccountApiController.DisconnectWalletAsync)}", new
            {
                AuthenticatedUser = authUser, 
                UserToEdit = userToEdit
            });
            
            return editResp;
        }
    }
}
