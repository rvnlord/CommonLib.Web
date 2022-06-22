using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Source.Models;
using CommonLib.Web.Source.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;

namespace CommonLib.Web.Source.Services.Account.Interfaces
{
    public interface IAccountClient
    {
        Task<ApiResponse<FindUserVM>> FindUserByNameAsync(string name);
        Task<ApiResponse<bool>> CheckUserManagerComplianceAsync(string userPropertyName, string userPropertyDisplayName, string userPropertyValue);
        Task<ApiResponse<FindUserVM>> FindUserByEmailAsync(string email);
        Task<ApiResponse<AuthenticateUserVM>> GetAuthenticatedUserAsync();
        Task<ApiResponse<RegisterUserVM>> RegisterAsync(RegisterUserVM userToRegister);
        Task<ApiResponse<ConfirmUserVM>> ConfirmEmailAsync(ConfirmUserVM userToConfirmEmail);
        Task<ApiResponse<FindUserVM>> FindUserByConfirmationCodeAsync(string activationCode);
        Task<ApiResponse<ResendConfirmationEmailUserVM>> ResendConfirmationEmailAsync(ResendConfirmationEmailUserVM resendConfirmationEmailUser);
        Task<ApiResponse<LoginUserVM>> LoginAsync(LoginUserVM userToLogin);
        Task<ApiResponse<LoginUserVM>> ExternalLoginAuthorizeAsync(LoginUserVM userToExternalLogin);
        Task<ApiResponse<IList<AuthenticationScheme>>> GetExternalAuthenticationSchemesAsync();
    }
}
