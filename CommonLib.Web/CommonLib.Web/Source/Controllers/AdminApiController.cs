using System;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Web.Source.Services.Admin.Interfaces;
using CommonLib.Web.Source.ViewModels.Account;
using CommonLib.Web.Source.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace CommonLib.Web.Source.Controllers
{
    [Route("api/admin"), ApiController]
    public class AdminApiController : MyControllerBase
    {
        private readonly IAdminManager _adminManager;

        public AdminApiController(IAdminManager adminManager)
        {
            _adminManager = adminManager;
        }

        [HttpPost("users")] // POST: api/admin/users
        public async Task<JToken> GetAllUsersAsync(JToken jAuthenticatedUser) => await EnsureResponseAsync(async () => await _adminManager.GetAllUsersAsync(jAuthenticatedUser.To<AuthenticateUserVM>()));
        
        [HttpPost("deleteuser")] // POST: api/admin/deleteuser
        public async Task<JToken> DeleteUserAsync(JToken jAuthenticatedUserAndUserToDelete) => await EnsureResponseAsync(async () => await _adminManager.DeleteUserAsync(jAuthenticatedUserAndUserToDelete["AuthenticatedUser"]?.To<AuthenticateUserVM>(), jAuthenticatedUserAndUserToDelete["UserToDelete"]?.To<AdminEditUserVM>()));
        
        [HttpPost("edituser")] // POST: api/admin/edituser
        public async Task<JToken> EditUserAsync(JToken jAuthenticatedUserAndUserToEdit) => await EnsureResponseAsync(async () => await _adminManager.EditUserAsync(jAuthenticatedUserAndUserToEdit["AuthenticatedUser"]?.To<AuthenticateUserVM>(), jAuthenticatedUserAndUserToEdit["UserToEdit"]?.To<AdminEditUserVM>()));
       
        [HttpPost("getroles")] // POST: api/admin/getroles
        public async Task<JToken> GetRolesAsync(JToken jAuthenticatedUser) => await EnsureResponseAsync(async () => await _adminManager.GetRolesAsync(jAuthenticatedUser.To<AuthenticateUserVM>()));
        
        [HttpPost("getclaims")] // POST: api/admin/getclaims
        public async Task<JToken> GetClaimsAsync(JToken jAuthenticatedUser) => await EnsureResponseAsync(async () => await _adminManager.GetClaimsAsync(jAuthenticatedUser.To<AuthenticateUserVM>()));

        [HttpPost("adduser")] // POST: api/admin/adduser
        public async Task<JToken> AddUserAsync(JToken jAuthenticatedUserAndUserToAdd) => await EnsureResponseAsync(async () => await _adminManager.AddUserAsync(jAuthenticatedUserAndUserToAdd["AuthenticatedUser"]?.To<AuthenticateUserVM>(), jAuthenticatedUserAndUserToAdd["UserToAdd"]?.To<AdminEditUserVM>()));

        [HttpPost("deleterole")] // POST: api/admin/deleterole
        public async Task<JToken> DeleteRoleAsync(JToken jAuthenticatedUserAndRoleToDelete) => await EnsureResponseAsync(async () => await _adminManager.DeleteRoleAsync(jAuthenticatedUserAndRoleToDelete["AuthenticatedUser"]?.To<AuthenticateUserVM>(), jAuthenticatedUserAndRoleToDelete["RoleToDelete"]?.To<AdminEditRoleVM>()));

        [HttpPost("addrole")] // POST: api/admin/addrole
        public async Task<JToken> AddRoleAsync(JToken jAuthenticatedUserAndRoleToAdd) => await EnsureResponseAsync(async () => await _adminManager.AddRoleAsync(jAuthenticatedUserAndRoleToAdd["AuthenticatedUser"]?.To<AuthenticateUserVM>(), jAuthenticatedUserAndRoleToAdd["RoleToAdd"]?.To<AdminEditRoleVM>()));
        
        [HttpPost("editrole")] // POST: api/admin/editrole
        public async Task<JToken> EditRoleAsync(JToken jAuthenticatedUserAndRoleToEdit) => await EnsureResponseAsync(async () => await _adminManager.EditRoleAsync(jAuthenticatedUserAndRoleToEdit["AuthenticatedUser"]?.To<AuthenticateUserVM>(), jAuthenticatedUserAndRoleToEdit["RoleToEdit"]?.To<AdminEditRoleVM>()));

        [HttpPost("findrolebyid")] // POST: api/admin/findrolebyid
        public async Task<JToken> FindRoleByIdAsync(JToken id) => await EnsureResponseAsync(async () => await _adminManager.FindRoleByIdAsync(id.To<Guid>()));
        
        [HttpPost("deleteclaim")] // POST: api/admin/deleteclaim
        public async Task<JToken> DeleteClaimAsync(JToken jAuthenticatedUserAndClaimToDelete) => await EnsureResponseAsync(async () => await _adminManager.DeleteClaimAsync(jAuthenticatedUserAndClaimToDelete["AuthenticatedUser"]?.To<AuthenticateUserVM>(), jAuthenticatedUserAndClaimToDelete["ClaimToDelete"]?.To<AdminEditClaimVM>()));

        [HttpPost("addclaim")] // POST: api/admin/addclaim
        public async Task<JToken> AddClaimAsync(JToken jAuthenticatedUserAndClaimToAdd) => await EnsureResponseAsync(async () => await _adminManager.AddClaimAsync(jAuthenticatedUserAndClaimToAdd["AuthenticatedUser"]?.To<AuthenticateUserVM>(), jAuthenticatedUserAndClaimToAdd["ClaimToAdd"]?.To<AdminEditClaimVM>()));

        [HttpPost("editclaim")] // POST: api/admin/editclaim
        public async Task<JToken> EditClaimAsync(JToken jAuthenticatedUserAndClaimToEdit) => await EnsureResponseAsync(async () => await _adminManager.EditClaimAsync(jAuthenticatedUserAndClaimToEdit["AuthenticatedUser"]?.To<AuthenticateUserVM>(), jAuthenticatedUserAndClaimToEdit["ClaimToEdit"]?.To<AdminEditClaimVM>()));
    }
}
