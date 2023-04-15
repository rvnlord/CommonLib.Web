using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Source.Models;
using CommonLib.Web.Source.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;

namespace CommonLib.Web.Source.Services.Account.Interfaces
{
    public interface IAccountClient
    {
        Task<ApiResponse<FindUserVM>> FindUserByIdAsync(Guid id, bool includeEmailClaim = false);
        Task<ApiResponse<FindUserVM>> FindUserByNameAsync(string name);
        Task<ApiResponse<FindUserVM>> FindUserByEmailAsync(string email);
        Task<ApiResponse<FindUserVM>> FindUserByConfirmationCodeAsync(string activationCode);
        Task<ApiResponse<FindRoleVM>> FindRoleByNameAsync(string roleName);
        Task<ApiResponse<FindClaimVM>> FindClaimByNameAsync(string claimName);
        Task<ApiResponse<List<ExternalLoginVM>>> GetExternalLogins(string userName);
        Task<ApiResponse<List<WalletVM>>> GetWalletsAsync(string userName);
        Task<ApiResponse<bool>> CheckUserManagerComplianceAsync(string userPropertyName, string userPropertyDisplayName, string userPropertyValue);
        Task<ApiResponse<AuthenticateUserVM>> GetAuthenticatedUserAsync();
        Task<ApiResponse<RegisterUserVM>> RegisterAsync(RegisterUserVM userToRegister);
        Task<ApiResponse<ConfirmUserVM>> ConfirmEmailAsync(ConfirmUserVM userToConfirmEmail);
        Task<ApiResponse<ResendConfirmationEmailUserVM>> ResendConfirmationEmailAsync(ResendConfirmationEmailUserVM resendConfirmationEmailUser);
        Task<ApiResponse<LoginUserVM>> LoginAsync(LoginUserVM userToLogin);
        Task<ApiResponse<LoginUserVM>> ExternalLoginAuthorizeAsync(LoginUserVM userToExternalLogin);
        Task<ApiResponse<LoginUserVM>> WalletLoginAsync(LoginUserVM userToWalletLogin);
        Task<ApiResponse<IList<AuthenticationScheme>>> GetExternalAuthenticationSchemesAsync();
        Task<ApiResponse<AuthenticateUserVM>> LogoutAsync();
        Task<ApiResponse<ForgotPasswordUserVM>> ForgotPasswordAsync(ForgotPasswordUserVM forgotPasswordUser);
        Task<ApiResponse<ResetPasswordUserVM>> ResetPasswordAsync(ResetPasswordUserVM resetPasswordUserVM);
        Task<ApiResponse<bool>> CheckUserResetPasswordCodeAsync(CheckResetPasswordCodeUserVM userToCheckResetPasswordCode);
        Task<ApiResponse<bool>> CheckUserPasswordAsync(CheckPasswordUserVM userToCheckPassword);
        Task<ApiResponse<EditUserVM>> EditAsync(EditUserVM user);
        Task<ApiResponse<FileData>> GetUserAvatarByNameAsync(string name);
        Task<ApiResponse<FileDataList>> FindAvatarsInUseAsync(bool includeFileData);
        Task<ApiResponse<EditUserVM>> ConnectExternalLoginAsync(EditUserVM userToEdit, LoginUserVM userToLogin);
        Task<ApiResponse<EditUserVM>> DisconnectExternalLoginAsync(EditUserVM userToEdit);
        Task<ApiResponse<EditUserVM>> ConnectWalletAsync(EditUserVM userToEdit, LoginUserVM userToLogin);
        Task<ApiResponse<EditUserVM>> DisconnectWalletAsync(EditUserVM editUserVm);
    }
}
