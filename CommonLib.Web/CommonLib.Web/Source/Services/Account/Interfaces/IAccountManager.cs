using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using CommonLib.Source.Models;
using CommonLib.Web.Source.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace CommonLib.Web.Source.Services.Account.Interfaces
{
    public interface IAccountManager
    {
        Task<ApiResponse<FindUserVM>> FindUserByNameAsync(string name);
        Task<ApiResponse<bool>> CheckUserManagerComplianceAsync(string userPropertyName, string userPropertyDisplayName, string userPropertyValue);
        Task<ApiResponse<FindUserVM>> FindUserByEmailAsync(string email);
        Task<ApiResponse<AuthenticateUserVM>> GetAuthenticatedUserAsync(HttpContext http, ClaimsPrincipal principal, AuthenticateUserVM user);
        Task<ApiResponse<RegisterUserVM>> RegisterAsync(RegisterUserVM userToRegister);
        Task<ApiResponse<ConfirmUserVM>> ConfirmEmailAsync(ConfirmUserVM userToConfirm);
        Task<string> GenerateLoginTicketAsync(Guid id, string passwordHash, bool rememberMe);
        Task<ApiResponse<FindUserVM>> FindUserByConfirmationCodeAsync(string confirmationCode);
        Task<ApiResponse<ResendConfirmationEmailUserVM>> ResendConfirmationEmailAsync(ResendConfirmationEmailUserVM userToResendConfirmationEmail);
        Task<ApiResponse<LoginUserVM>> LoginAsync(LoginUserVM userToLogin);
        Task<(AuthenticationProperties authenticationProperties, string schemaName)> ExternalLoginAsync(LoginUserVM userToExternalLogin);
        Task<string> ExternalLoginCallbackAsync(string returnUrl, string remoteError);
        Task<ApiResponse<LoginUserVM>> ExternalLoginAuthorizeAsync(LoginUserVM userToExternalLogin);
        Task<ApiResponse<IList<AuthenticationScheme>>> GetExternalAuthenticationSchemesAsync();
    }
}
