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
        Task<ApiResponse<FindUserVM>> FindUserByIdAsync(Guid id, bool includeEmailClaim = false);
        Task<ApiResponse<FindUserVM>> FindUserByNameAsync(string name);
        Task<ApiResponse<FindUserVM>> FindUserByEmailAsync(string email);
        Task<ApiResponse<FindUserVM>> FindUserByConfirmationCodeAsync(string confirmationCode);
        Task<ApiResponse<FindRoleVM>> FindRoleByNameAsync(string roleName);
        Task<ApiResponse<FindClaimVM>> FindClaimByNameAsync(string claimName);
        Task<ApiResponse<List<ExternalLoginVM>>> GetExternalLoginsAsync(string userName);
        Task<ApiResponse<List<WalletVM>>> GetWalletsAsync(string userName);
        Task<ApiResponse<bool>> CheckUserManagerComplianceAsync(string userPropertyName, string userPropertyDisplayName, string userPropertyValue);
        Task<ApiResponse<AuthenticateUserVM>> GetAuthenticatedUserAsync(HttpContext http, ClaimsPrincipal principal, AuthenticateUserVM user);
        Task<ApiResponse<RegisterUserVM>> RegisterAsync(RegisterUserVM userToRegister, bool autoConfirmEmail = false);
        Task<ApiResponse<ConfirmUserVM>> ConfirmEmailAsync(ConfirmUserVM userToConfirm);
        Task<string> GenerateLoginTicketAsync(Guid id, string passwordHash, bool rememberMe);
        Task<ApiResponse<ResendConfirmationEmailUserVM>> ResendConfirmationEmailAsync(ResendConfirmationEmailUserVM userToResendConfirmationEmail);
        Task<ApiResponse<LoginUserVM>> LoginAsync(LoginUserVM userToLogin);
        Task<(AuthenticationProperties authenticationProperties, string schemaName)> ExternalLoginAsync(LoginUserVM userToExternalLogin);
        Task<string> ExternalLoginCallbackAsync(string returnUrl, string remoteError);
        Task<ApiResponse<LoginUserVM>> ExternalLoginAuthorizeAsync(LoginUserVM userToExternalLogin);
        Task<ApiResponse<LoginUserVM>> WalletLoginAsync(LoginUserVM userToWalletLogin);
        Task<ApiResponse<IList<AuthenticationScheme>>> GetExternalAuthenticationSchemesAsync();
        Task<ApiResponse<AuthenticateUserVM>> LogoutAsync(AuthenticateUserVM userToLogout);
        Task<ApiResponse<ForgotPasswordUserVM>> ForgotPasswordAsync(ForgotPasswordUserVM userWithForgottenPassword);
        Task<ApiResponse<ResetPasswordUserVM>> ResetPasswordAsync(ResetPasswordUserVM userToResetPassword);
        Task<ApiResponse<bool>> CheckUserResetPasswordCodeAsync(CheckResetPasswordCodeUserVM userToCheckResetPasswordCode);
        Task<ApiResponse<bool>> CheckUserPasswordAsync(CheckPasswordUserVM userToCheckPassword);
        Task<ApiResponse<EditUserVM>> EditAsync(AuthenticateUserVM authUser, EditUserVM userToEdit);
        Task<ApiResponse<FileData>> GetUserAvatarByNameAsync(string name);
        Task<ApiResponse<FileDataList>> FindAvatarsInUseAsync(bool includeData);
        Task<ApiResponse<EditUserVM>> ConnectExternalLoginAsync(AuthenticateUserVM authUser, EditUserVM userToEdit, LoginUserVM userToLogin);
        Task<ApiResponse<EditUserVM>> DisconnectExternalLoginAsync(AuthenticateUserVM authUser, EditUserVM userToEdit);
        Task<ApiResponse<EditUserVM>> ConnectWalletAsync(AuthenticateUserVM authUser, EditUserVM userToEdit, LoginUserVM userToLogin);
        Task<ApiResponse<EditUserVM>> DisconnectWalletAsync(AuthenticateUserVM authUser, EditUserVM userToEdit);
    }
}
