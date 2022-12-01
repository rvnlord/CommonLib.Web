using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Source.Models;
using CommonLib.Web.Source.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace CommonLib.Web.Source.Services.Account.Interfaces
{
    public interface IAccountManager
    {
        Task<ApiResponse<FindUserVM>> FindUserByIdAsync(Guid id);
        Task<ApiResponse<FindUserVM>> FindUserByNameAsync(string name);
        Task<ApiResponse<FindUserVM>> FindUserByEmailAsync(string email);
        Task<ApiResponse<FindUserVM>> FindUserByConfirmationCodeAsync(string confirmationCode);
        Task<ApiResponse<FindRoleVM>> FindRoleByNameAsync(string roleName);
        Task<ApiResponse<FindClaimVM>> FindClaimByNameAsync(string claimName);
        Task<ApiResponse<bool>> CheckUserManagerComplianceAsync(string userPropertyName, string userPropertyDisplayName, string userPropertyValue);
        Task<ApiResponse<AuthenticateUserVM>> GetAuthenticatedUserAsync(HttpContext http, ClaimsPrincipal principal, AuthenticateUserVM user);
        Task<ApiResponse<RegisterUserVM>> RegisterAsync(RegisterUserVM userToRegister);
        Task<ApiResponse<ConfirmUserVM>> ConfirmEmailAsync(ConfirmUserVM userToConfirm);
        Task<string> GenerateLoginTicketAsync(Guid id, string passwordHash, bool rememberMe);
        Task<ApiResponse<ResendConfirmationEmailUserVM>> ResendConfirmationEmailAsync(ResendConfirmationEmailUserVM userToResendConfirmationEmail);
        Task<ApiResponse<LoginUserVM>> LoginAsync(LoginUserVM userToLogin);
        Task<(AuthenticationProperties authenticationProperties, string schemaName)> ExternalLoginAsync(LoginUserVM userToExternalLogin);
        Task<string> ExternalLoginCallbackAsync(string returnUrl, string remoteError);
        Task<ApiResponse<LoginUserVM>> ExternalLoginAuthorizeAsync(LoginUserVM userToExternalLogin);
        Task<ApiResponse<IList<AuthenticationScheme>>> GetExternalAuthenticationSchemesAsync();
        Task<ApiResponse<AuthenticateUserVM>> LogoutAsync(AuthenticateUserVM userToLogout);
        Task<ApiResponse<ForgotPasswordUserVM>> ForgotPasswordAsync(ForgotPasswordUserVM userWithForgottenPassword);
        Task<ApiResponse<ResetPasswordUserVM>> ResetPasswordAsync(ResetPasswordUserVM userToResetPassword);
        Task<ApiResponse<bool>> CheckUserResetPasswordCodeAsync(CheckResetPasswordCodeUserVM userToCheckResetPasswordCode);
        Task<ApiResponse<bool>> CheckUserPasswordAsync(CheckPasswordUserVM userToCheckPassword);
        Task<ApiResponse<EditUserVM>> EditAsync(AuthenticateUserVM authUser, EditUserVM userToEdit);
        Task<ApiResponse<FileData>> GetUserAvatarByNameAsync(string name);
    }
}
