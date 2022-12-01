using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Models;
using CommonLib.Web.Source.Services.Account.Interfaces;
using CommonLib.Web.Source.Services.Admin.Interfaces;
using CommonLib.Web.Source.ViewModels.Account;
using CommonLib.Web.Source.ViewModels.Admin;
using Microsoft.JSInterop;

namespace CommonLib.Web.Source.Services.Admin
{
    public class AdminClient : IAdminClient
    {
        private readonly HttpClient _httpClient;
        private readonly IAccountClient _accountClient;
        private readonly IJSRuntime _jsRuntime;
        private readonly ILocalStorageService _localStorage;

        public HttpClient HttpClient
        {
            get
            {
                if (_httpClient.BaseAddress == null && ConfigUtils.BackendBaseUrl != null)
                    _httpClient.BaseAddress = new Uri(ConfigUtils.BackendBaseUrl);
                return _httpClient;
            }
            init => _httpClient = value;
        }

        public AdminClient(HttpClient httpClient, IAccountClient accountService, IJSRuntime jsRuntime, ILocalStorageService localStorage)
        {
            HttpClient = httpClient;
            _accountClient = accountService;
            _jsRuntime = jsRuntime;
            _localStorage = localStorage;
        }

        public async Task<ApiResponse<List<FindUserVM>>> GetAllUsersAsync()
        {
            var authUser = (await _accountClient.GetAuthenticatedUserAsync())?.Result;
            var usersResp = await HttpClient.PostJTokenAsync<ApiResponse<List<FindUserVM>>>("api/admin/users", authUser);
            usersResp.Result ??= new List<FindUserVM>();
            return usersResp;
        }

        public async Task<ApiResponse<AdminEditUserVM>> DeleteUserAsync(AdminEditUserVM userToDelete)
        {
            var authUser = (await _accountClient.GetAuthenticatedUserAsync())?.Result;
            return await HttpClient.PostJTokenAsync<ApiResponse<AdminEditUserVM>>("api/admin/deleteuser", new
            {
                AuthenticatedUser = authUser,
                UserToDelete = userToDelete
            });
        }

        public async Task<ApiResponse<AdminEditUserVM>> EditUserAsync(AdminEditUserVM userToEdit)
        {
            var authUser = (await _accountClient.GetAuthenticatedUserAsync())?.Result;
            var editUserResp = await HttpClient.PostJTokenAsync<ApiResponse<AdminEditUserVM>>("api/admin/edituser", new
            {
                AuthenticatedUser = authUser,
                UserToEdit = userToEdit
            });

            if (editUserResp.IsError || editUserResp.Result?.Ticket == null)
                return editUserResp;

            if (authUser?.RememberMe == true)
            {
                await _jsRuntime.InvokeVoidAsync("Cookies.set", "Ticket", editUserResp.Result.Ticket, new { expires = 365 * 24 * 60 * 60 });
                await _localStorage.SetItemAsync("Ticket", editUserResp.Result.Ticket);
            }
            else
            {
                await _jsRuntime.InvokeVoidAsync("Cookies.set", "Ticket", editUserResp.Result.Ticket);
                await _localStorage.RemoveItemAsync("Ticket");
            }

            return editUserResp;
        }

        public async Task<ApiResponse<List<FindRoleVM>>> GetRolesAsync()
        {
            var authUser = (await _accountClient.GetAuthenticatedUserAsync())?.Result;
            var rolesResp = await HttpClient.PostJTokenAsync<ApiResponse<List<FindRoleVM>>>("api/admin/getroles", authUser);
            rolesResp.Result ??= new List<FindRoleVM>();
            return rolesResp;
        }

        public async Task<ApiResponse<List<FindClaimVM>>> GetClaimsAsync()
        {
            var authUser = (await _accountClient.GetAuthenticatedUserAsync())?.Result;
            var claimsResp = await HttpClient.PostJTokenAsync<ApiResponse<List<FindClaimVM>>>("api/admin/getclaims", authUser);
            claimsResp.Result ??= new List<FindClaimVM>(); // by default json serializer will serialize an empty array/list as null, so if there are no claims, we will get null from the api 
            return claimsResp;
        }

        public async Task<ApiResponse<AdminEditUserVM>> AddUserAsync(AdminEditUserVM userToAdd)
        {
            var authUser = (await _accountClient.GetAuthenticatedUserAsync())?.Result;
            return await HttpClient.PostJTokenAsync<ApiResponse<AdminEditUserVM>>("api/admin/adduser", new
            {
                AuthenticatedUser = authUser,
                UserToAdd = userToAdd
            });
        }
        
        public async Task<ApiResponse<AdminEditRoleVM>> DeleteRoleAsync(AdminEditRoleVM roleToDelete)
        {
            var authUser = (await _accountClient.GetAuthenticatedUserAsync())?.Result;
            return await HttpClient.PostJTokenAsync<ApiResponse<AdminEditRoleVM>>("api/admin/deleterole", new
            {
                AuthenticatedUser = authUser,
                RoleToDelete = roleToDelete
            });
        }

        public async Task<ApiResponse<AdminEditRoleVM>> AddRoleAsync(AdminEditRoleVM roleToAdd)
        {
            var authUser = (await _accountClient.GetAuthenticatedUserAsync())?.Result;
            return await HttpClient.PostJTokenAsync<ApiResponse<AdminEditRoleVM>>("api/admin/addrole", new
            {
                AuthenticatedUser = authUser,
                RoleToAdd = roleToAdd
            });
        }

        public async Task<ApiResponse<AdminEditRoleVM>> EditRoleAsync(AdminEditRoleVM roleToEdit)
        {
            var authUser = (await _accountClient.GetAuthenticatedUserAsync())?.Result;
            return await HttpClient.PostJTokenAsync<ApiResponse<AdminEditRoleVM>>("api/admin/editrole", new
            {
                AuthenticatedUser = authUser,
                RoleToEdit = roleToEdit
            });
        }

        public async Task<ApiResponse<FindRoleVM>> FindRoleByIdAsync(Guid id)
        {
            return await HttpClient.PostJTokenAsync<ApiResponse<FindRoleVM>>("api/admin/findrolebyid", id);
        }
        
        public async Task<ApiResponse<AdminEditClaimVM>> DeleteClaimAsync(AdminEditClaimVM claimToDelete)
        {
            var authUser = (await _accountClient.GetAuthenticatedUserAsync())?.Result;
            return await HttpClient.PostJTokenAsync<ApiResponse<AdminEditClaimVM>>("api/admin/deleteclaim", new
            {
                AuthenticatedUser = authUser,
                ClaimToDelete = claimToDelete
            });
        }

        public async Task<ApiResponse<AdminEditClaimVM>> AddClaimAsync(AdminEditClaimVM claimToAdd)
        {
            var authUser = (await _accountClient.GetAuthenticatedUserAsync())?.Result;
            return await HttpClient.PostJTokenAsync<ApiResponse<AdminEditClaimVM>>("api/admin/addclaim", new
            {
                AuthenticatedUser = authUser,
                ClaimToAdd = claimToAdd
            });
        }

        public async Task<ApiResponse<FindUserVM>> FindUserByIdAsync(Guid id)
        {
            return await HttpClient.PostJTokenAsync<ApiResponse<FindUserVM>>("api/admin/finduserbyid", id);
        }

        public async Task<ApiResponse<AdminEditClaimVM>> EditClaimAsync(AdminEditClaimVM claimToedit)
        {
            var authUser = (await _accountClient.GetAuthenticatedUserAsync())?.Result;
            return await HttpClient.PostJTokenAsync<ApiResponse<AdminEditClaimVM>>("api/admin/editclaim", new
            {
                AuthenticatedUser = authUser,
                ClaimToEdit = claimToedit
            });
        }
    }
}
