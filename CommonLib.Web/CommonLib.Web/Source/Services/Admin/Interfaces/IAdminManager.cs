using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Source.Models;
using CommonLib.Web.Source.ViewModels.Account;
using CommonLib.Web.Source.ViewModels.Admin;

namespace CommonLib.Web.Source.Services.Admin.Interfaces
{
    public interface IAdminManager
    {
        Task<ApiResponse<List<FindUserVM>>> GetAllUsersAsync(AuthenticateUserVM authUser);
        Task<ApiResponse<AdminEditUserVM>> DeleteUserAsync(AuthenticateUserVM authUser, AdminEditUserVM userToDelete);
        Task<ApiResponse<AdminEditUserVM>> EditUserAsync(AuthenticateUserVM authUser, AdminEditUserVM userToEdit);
        Task<ApiResponse<List<FindRoleVM>>> GetRolesAsync(AuthenticateUserVM authUser);
        Task<ApiResponse<List<FindClaimVM>>> GetClaimsAsync(AuthenticateUserVM authUser);
        Task<ApiResponse<AdminEditUserVM>> AddUserAsync(AuthenticateUserVM authUser, AdminEditUserVM userToAdd);
        Task<ApiResponse<AdminEditRoleVM>> DeleteRoleAsync(AuthenticateUserVM authUser, AdminEditRoleVM roleToDelete);
        Task<ApiResponse<AdminEditRoleVM>> AddRoleAsync(AuthenticateUserVM authUser, AdminEditRoleVM roleToAdd);
        Task<ApiResponse<AdminEditRoleVM>> EditRoleAsync(AuthenticateUserVM authUser, AdminEditRoleVM roleToEdit);
        Task<ApiResponse<FindRoleVM>> FindRoleByIdAsync(Guid id);
        Task<ApiResponse<AdminEditClaimVM>> DeleteClaimAsync(AuthenticateUserVM authUser, AdminEditClaimVM claimToDelete);
        Task<ApiResponse<bool>> HasClaimAsync(FindUserVM user, string claimName);
        Task<ApiResponse<AdminEditClaimVM>> AddClaimAsync(AuthenticateUserVM authUser, AdminEditClaimVM claimToAdd);
        Task<ApiResponse<AdminEditClaimVM>> EditClaimAsync(AuthenticateUserVM authUser, AdminEditClaimVM claimToEdit);
    }
}
