using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Source.Models;
using CommonLib.Web.Source.ViewModels.Account;
using CommonLib.Web.Source.ViewModels.Admin;

namespace CommonLib.Web.Source.Services.Admin.Interfaces
{
    public interface IAdminClient
    {
        Task<ApiResponse<List<FindUserVM>>> GetAllUsersAsync();
        Task<ApiResponse<AdminEditUserVM>> DeleteUserAsync(AdminEditUserVM user);
        Task<ApiResponse<AdminEditUserVM>> EditUserAsync(AdminEditUserVM user);
        Task<ApiResponse<List<FindRoleVM>>> GetRolesAsync();
        Task<ApiResponse<List<FindClaimVM>>> GetClaimsAsync();
        Task<ApiResponse<AdminEditUserVM>> AddUserAsync(AdminEditUserVM user);
        Task<ApiResponse<FindRoleVM>> FindRoleByNameAsync(string roleName);
        Task<ApiResponse<AdminEditRoleVM>> DeleteRoleAsync(AdminEditRoleVM role);
        Task<ApiResponse<AdminEditRoleVM>> AddRoleAsync(AdminEditRoleVM role);
        Task<ApiResponse<AdminEditRoleVM>> EditRoleAsync(AdminEditRoleVM role);
        Task<ApiResponse<FindRoleVM>> FindRoleByIdAsync(Guid id);
        Task<ApiResponse<FindClaimVM>> FindClaimByNameAsync(string claimName);
        Task<ApiResponse<AdminEditClaimVM>> DeleteClaimAsync(AdminEditClaimVM claim);
        Task<ApiResponse<AdminEditClaimVM>> AddClaimAsync(AdminEditClaimVM claim);
        Task<ApiResponse<FindUserVM>> FindUserByIdAsync(Guid id);
        Task<ApiResponse<AdminEditClaimVM>> EditClaimAsync(AdminEditClaimVM claim);
    }
}
