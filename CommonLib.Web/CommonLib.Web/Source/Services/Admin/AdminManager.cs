using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Models;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.DbContext;
using CommonLib.Web.Source.DbContext.Models.Account;
using CommonLib.Web.Source.Services.Account.Interfaces;
using CommonLib.Web.Source.Services.Admin.Interfaces;
using CommonLib.Web.Source.ViewModels.Account;
using CommonLib.Web.Source.ViewModels.Admin;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommonLib.Web.Source.Services.Admin
{
    public class AdminManager : IAdminManager
    {
        private readonly UserManager<DbUser> _userManager;
        private readonly IMapper _mapper;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly AccountDbContext _db;
        private readonly IPasswordHasher<DbUser> _passwordHasher;
        private readonly IAccountManager _accountManager;

        public AdminManager(UserManager<DbUser> userManager, IMapper mapper, RoleManager<IdentityRole<Guid>> roleManager, AccountDbContext db, IPasswordHasher<DbUser> passwordHasher, IAccountManager accountManager)
        {
            _userManager = userManager;
            _mapper = mapper;
            _roleManager = roleManager;
            _db = db;
            _passwordHasher = passwordHasher;
            _accountManager = accountManager;
        }

        public async Task<ApiResponse<List<FindUserVM>>> GetAllUsersAsync(AuthenticateUserVM authUser)
        {
            if (authUser is null || !authUser.HasAuthenticationStatus(AuthStatus.Authenticated) || !authUser.HasRole("Admin"))
                return new ApiResponse<List<FindUserVM>>(StatusCodeType.Status401Unauthorized, "You are not Authorized to Access Users' Data", null);

            var users = await _userManager.Users.Include(u => u.Avatar).ToListAsync();
            var userToFind = new List<FindUserVM>();
            foreach (var user in users)
            {
                var userToEditByAdmin = _mapper.Map(user, new FindUserVM());
                userToEditByAdmin.Roles = (await _userManager.GetRolesAsync(user)).Select(r => FindRoleByName(r).Result).ToList();
                userToEditByAdmin.Claims = (await _userManager.GetClaimsAsync(user)).Select(c => FindClaimByName(c.Type).Result).Where(c => !c.Name.EqualsIgnoreCase("Email")).ToList();
                userToFind.Add(userToEditByAdmin);
            }

            return new ApiResponse<List<FindUserVM>>(StatusCodeType.Status201Created, "Successfully Retrieved Users", null, userToFind);
        }

        public async Task<ApiResponse<AdminEditUserVM>> DeleteUserAsync(AuthenticateUserVM authUser, AdminEditUserVM userToDelete)
        {
            if (authUser == null || !authUser.HasAuthenticationStatus(AuthStatus.Authenticated) || !authUser.HasRole("Admin"))
                return new ApiResponse<AdminEditUserVM>(StatusCodeType.Status401Unauthorized, "You are not Authorized to Delete Users", null);
            if (authUser.Id == userToDelete.Id)
                return new ApiResponse<AdminEditUserVM>(StatusCodeType.Status401Unauthorized, "You can't Delete yourself", null);
            if (userToDelete.Id == default)
                return new ApiResponse<AdminEditUserVM>(StatusCodeType.Status400BadRequest, "Id for the User was not supplied, as it is done automatically it should never happen", null);

            var user = await _userManager.FindByIdAsync(userToDelete.Id.ToString());
            if (user == null)
                return new ApiResponse<AdminEditUserVM>(StatusCodeType.Status400BadRequest, $"User with Id: \"{userToDelete.Id}\" was not found, it should never happen", null);

            var deleteUserResponse = await _userManager.DeleteAsync(user);
            if (!deleteUserResponse.Succeeded)
                return new ApiResponse<AdminEditUserVM>(StatusCodeType.Status400BadRequest, $"Deleting User with Id: \"{userToDelete.Id}\" Failed. ({deleteUserResponse.FirstError()})", null);

            userToDelete.IsDeleted = true;
            return new ApiResponse<AdminEditUserVM>(StatusCodeType.Status201Created, $"Successfully Deleted User: \"{userToDelete.UserName}\"", null, userToDelete);
        }

        public async Task<ApiResponse<AdminEditUserVM>> EditUserAsync(AuthenticateUserVM authUser, AdminEditUserVM userToEdit)
        {
            if (authUser == null || !authUser.HasAuthenticationStatus(AuthStatus.Authenticated) || !authUser.HasRole("Admin"))
                return new ApiResponse<AdminEditUserVM>(StatusCodeType.Status401Unauthorized, "You are not Authorized to Edit Users", null);
            if (userToEdit.Id == authUser.Id && !userToEdit.HasRole("Admin"))
                return new ApiResponse<AdminEditUserVM>(StatusCodeType.Status401Unauthorized, "You can't remove \"Admin\" Role from your own Account", null);
            if (userToEdit.Id == default)
                return new ApiResponse<AdminEditUserVM>(StatusCodeType.Status400BadRequest, "Id for the User was not supplied, as it is done automatically it should never happen", null);
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == userToEdit.Id);
            if (user == null)
                return new ApiResponse<AdminEditUserVM>(StatusCodeType.Status400BadRequest, $"User with Id: \"{userToEdit.Id}\" was not found, it should never happen", null);

            user.Email = userToEdit.Email.IsNullOrWhiteSpace() ? user.Email : userToEdit.Email;
            user.UserName = userToEdit.UserName.IsNullOrWhiteSpace() ? user.UserName : userToEdit.UserName;
            user.EmailConfirmed = userToEdit.IsConfirmed;
            var updateuserResp = await _userManager.UpdateAsync(user);
            if (!updateuserResp.Succeeded)
                return new ApiResponse<AdminEditUserVM>(StatusCodeType.Status400BadRequest, $"Editing User with Id: \"{userToEdit.Id}\" Failed", updateuserResp.Errors.ToLookup(userToEdit.GetPropertyNames()));

            if (!userToEdit.Password.IsNullOrWhiteSpace() && _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, userToEdit.Password) != PasswordVerificationResult.Success) // if admin changed password to a new one
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, userToEdit.Password);
                await _db.SaveChangesAsync();
                if (userToEdit.Id == authUser.Id)
                    userToEdit.Ticket = await _accountManager.GenerateLoginTicketAsync(user.Id, user.PasswordHash, authUser.RememberMe);
            }

            var removeRolesResp = await _userManager.RemoveFromRolesAsync(user, await _userManager.GetRolesAsync(user));
            var editRolesResp = await _userManager.AddToRolesAsync(user, userToEdit.Roles.Select(r => r.Name));
            if (!removeRolesResp.Succeeded || !editRolesResp.Succeeded)
                return new ApiResponse<AdminEditUserVM>(StatusCodeType.Status400BadRequest, $"Editing User with Id: \"{userToEdit.Id}\" Succeeded, but modifying Roles Failed. ({(!removeRolesResp.Succeeded ? removeRolesResp.FirstError() : editRolesResp.FirstError())})", null);

            var removeClaimsResp = await _userManager.RemoveClaimsAsync(user, await _userManager.GetClaimsAsync(user));
            var editClaimsResp = await _userManager.AddClaimsAsync(user, userToEdit.Claims.Select(c => new Claim(c.Name, c.Values.First().Value))); // we don't consider values for simplicity sake so we can tak any (first) available
            if (!removeClaimsResp.Succeeded || !editClaimsResp.Succeeded)
                return new ApiResponse<AdminEditUserVM>(StatusCodeType.Status400BadRequest, $"Editing User with Id: \"{userToEdit.Id}\" Succeeded, but modifying Claims Failed. ({(!removeClaimsResp.Succeeded ? removeClaimsResp.FirstError() : editClaimsResp.FirstError())})", null);

            return new ApiResponse<AdminEditUserVM>(StatusCodeType.Status201Created, $"Successfully Modified User: \"{userToEdit.UserName}\"", null, userToEdit);
        }

        public async Task<ApiResponse<List<FindRoleVM>>> GetRolesAsync(AuthenticateUserVM authUser)
        {
            if (authUser == null || !authUser.HasAuthenticationStatus(AuthStatus.Authenticated) || !authUser.HasRole("Admin"))
                return new ApiResponse<List<FindRoleVM>>(StatusCodeType.Status401Unauthorized, "You are not Authorized to Access Roles", null);

            var foundRoles = (await _roleManager.Roles.ToListAsync()).Select(rl => new FindRoleVM
            {
                Id = rl.Id,
                Name = rl.Name,
                UserNames = (
                    from ur in _db.UserRoles
                    join r in _db.Roles on ur.RoleId equals r.Id
                    join u in _db.Users on ur.UserId equals u.Id
                    where rl.Id == r.Id
                    select u.UserName).ToList()
            }).ToList();

            return new ApiResponse<List<FindRoleVM>>(StatusCodeType.Status201Created, "Successfully Retrieved Roles", null, foundRoles);
        }

        public async Task<ApiResponse<List<FindClaimVM>>> GetClaimsAsync(AuthenticateUserVM authUser)
        {
            if (authUser == null || !authUser.HasAuthenticationStatus(AuthStatus.Authenticated) || !authUser.HasRole("Admin"))
                return new ApiResponse<List<FindClaimVM>>(StatusCodeType.Status401Unauthorized, "You are not Authorized to Access Claims", null);

            var claims = (
                from uc in await _db.UserClaims.ToListAsync()
                group uc by uc.ClaimType
                into claimsByType
                select new FindClaimVM
                {
                    Name = claimsByType.Key,
                    Values = (
                        from cbt in claimsByType
                        group cbt by cbt.ClaimValue
                        into claimByTypeByValue
                        select new FindClaimValueVM
                        {
                            Value = claimByTypeByValue.Key,
                            UserNames = (
                                from cbtbv in claimByTypeByValue
                                join u in _db.Users.ToList() on cbtbv.UserId equals u.Id
                                select u.UserName).ToList()
                        }).ToList()
                }).ToList();

            return new ApiResponse<List<FindClaimVM>>(StatusCodeType.Status200OK, "Successfully Retrieved Claims", null, claims);
        }

        public async Task<ApiResponse<AdminEditUserVM>> AddUserAsync(AuthenticateUserVM authUser, AdminEditUserVM userToAdd)
        {
            if (authUser == null || !authUser.HasAuthenticationStatus(AuthStatus.Authenticated) || !authUser.HasRole("Admin"))
                return new ApiResponse<AdminEditUserVM>(StatusCodeType.Status401Unauthorized, "You are not Authorized to Add Users", null);

            var user = new DbUser
            {
                UserName = userToAdd.UserName,
                Email = userToAdd.Email,
                EmailConfirmed = userToAdd.IsConfirmed,
            };

            var addUserResp = await _userManager.CreateAsync(user);
            if (!addUserResp.Succeeded)
                return new ApiResponse<AdminEditUserVM>(StatusCodeType.Status400BadRequest, $"Adding User \"{userToAdd.UserName}\" Failed. ({addUserResp.FirstError()})", null);

            if (!userToAdd.Password.IsNullOrWhiteSpace())
            {
                user = await _db.Users.SingleOrDefaultAsync(u => u.UserName.ToLower() == userToAdd.UserName.ToLower());
                if (user is not null)
                {
                    user.PasswordHash = _passwordHasher.HashPassword(user, userToAdd.Password); // use db directly to override identity constraints, we are admin after all here
                    await _db.SaveChangesAsync();
                }
            }

            var addRolesResp = await _userManager.AddToRolesAsync(user, userToAdd.Roles.Select(r => r.Name));
            if (!addRolesResp.Succeeded)
                return new ApiResponse<AdminEditUserVM>(StatusCodeType.Status400BadRequest, $"Adding User \"{userToAdd.UserName}\" Succeeded, but modifying Roles Failed. ({addRolesResp.FirstError()})", null);

            var addClaimsResp = await _userManager.AddClaimsAsync(user, userToAdd.Claims.Select(c => new Claim(c.Name, c.Values.First().Value))); // we don't consider values for simplicity sake so we can tak any (first) available
            if (!addClaimsResp.Succeeded)
                return new ApiResponse<AdminEditUserVM>(StatusCodeType.Status400BadRequest, $"Editing User with Id: \"{userToAdd.Id}\" Succeeded, but modifying Claims Failed. ({addClaimsResp.FirstError()})", null);

            return new ApiResponse<AdminEditUserVM>(StatusCodeType.Status201Created, $"Successfully Added User: \"{userToAdd.UserName}\"", null, userToAdd);
        }

        public async Task<ApiResponse<FindRoleVM>> FindRoleByNameAsync(string roleName)
        {
            var role = await _db.Roles.SingleOrDefaultAsync(r => r.Name.ToLower() == roleName.ToLower());
            if (role == null)
                return new ApiResponse<FindRoleVM>(StatusCodeType.Status404NotFound, "There is no Role with the given Name", null);

            var foundRole = _mapper.Map(role, new FindRoleVM());
            return new ApiResponse<FindRoleVM>(StatusCodeType.Status200OK, "Role Found", null, foundRole);
        }

        public ApiResponse<FindRoleVM> FindRoleByName(string roleName)
        {
            var role = _db.Roles.SingleOrDefault(r => r.Name.ToLower() == roleName.ToLower());
            if (role == null)
                return new ApiResponse<FindRoleVM>(StatusCodeType.Status404NotFound, "There is no Role with the given Name", null);

            var foundRole = _mapper.Map(role, new FindRoleVM());
            return new ApiResponse<FindRoleVM>(StatusCodeType.Status200OK, "Role Found", null, foundRole);
        }

        public async Task<ApiResponse<AdminEditRoleVM>> DeleteRoleAsync(AuthenticateUserVM authUser, AdminEditRoleVM roleToDelete)
        {
            if (authUser == null || !authUser.HasAuthenticationStatus(AuthStatus.Authenticated) || !authUser.HasRole("Admin"))
                return new ApiResponse<AdminEditRoleVM>(StatusCodeType.Status401Unauthorized, "You are not Authorized to Delete Roles", null);
            if (roleToDelete.Name.IsNullOrWhiteSpace())
                return new ApiResponse<AdminEditRoleVM>(StatusCodeType.Status400BadRequest, "Name for the Role was not supplied, as it is done automatically it should never happen", null);
            if (roleToDelete.Name.EqualsIgnoreCase("Admin"))
                return new ApiResponse<AdminEditRoleVM>(StatusCodeType.Status400BadRequest, "You can't remove \"Admin\" role, it would be just dumb", null);
            var role = await _roleManager.FindByNameAsync(roleToDelete.Name);
            if (role == null)
                return new ApiResponse<AdminEditRoleVM>(StatusCodeType.Status400BadRequest, $"Role \"{roleToDelete.Name}\" was not found, it should never happen", null);

            var deleteRoleResponse = await _roleManager.DeleteAsync(role);
            if (!deleteRoleResponse.Succeeded)
                return new ApiResponse<AdminEditRoleVM>(StatusCodeType.Status400BadRequest, $"Deleting Role \"{roleToDelete.Name}\" Failed. ({deleteRoleResponse.FirstError()})", null);

            return new ApiResponse<AdminEditRoleVM>(StatusCodeType.Status201Created, $"Successfully Deleted Role \"{roleToDelete.Name}\"", null, roleToDelete);
        }

        public async Task<ApiResponse<AdminEditRoleVM>> AddRoleAsync(AuthenticateUserVM authUser, AdminEditRoleVM roleToAdd)
        {
            if (authUser == null || !authUser.HasAuthenticationStatus(AuthStatus.Authenticated) || !authUser.HasRole("Admin"))
                return new ApiResponse<AdminEditRoleVM>(StatusCodeType.Status401Unauthorized, "You are not Authorized to Add Roles", null);

            var role = new IdentityRole<Guid> { Name = roleToAdd.Name };

            var addRoleResp = await _roleManager.CreateAsync(role);
            if (!addRoleResp.Succeeded)
                return new ApiResponse<AdminEditRoleVM>(StatusCodeType.Status400BadRequest, $"Adding Role \"{roleToAdd.Name}\" Failed. ({addRoleResp.FirstError()})", null);

            foreach (var userName in roleToAdd.UserNames)
            {
                var user = await _userManager.FindByNameAsync(userName);
                if (user == null)
                    return new ApiResponse<AdminEditRoleVM>(StatusCodeType.Status400BadRequest, $"Adding Role \"{roleToAdd.Name}\" to User \"{userName}\" Failed, there is no such User", null);

                var addRoleToUserResp = await _userManager.AddToRoleAsync(user, role.Name);
                if (!addRoleToUserResp.Succeeded)
                    return new ApiResponse<AdminEditRoleVM>(StatusCodeType.Status400BadRequest, $"Adding Role \"{roleToAdd.Name}\" to User \"{userName}\" Failed. ({addRoleResp.FirstError()})", null);
            }

            return new ApiResponse<AdminEditRoleVM>(StatusCodeType.Status201Created, $"Successfully Added Role: \"{roleToAdd.Name}\"", null, roleToAdd);
        }

        public async Task<ApiResponse<AdminEditRoleVM>> EditRoleAsync(AuthenticateUserVM authUser, AdminEditRoleVM roleToEdit)
        {
            if (authUser == null || !authUser.HasAuthenticationStatus(AuthStatus.Authenticated) || !authUser.HasRole("Admin"))
                return new ApiResponse<AdminEditRoleVM>(StatusCodeType.Status401Unauthorized, "You are not Authorized to Edit Roles", null);
            if (roleToEdit.Id == default)
                return new ApiResponse<AdminEditRoleVM>(StatusCodeType.Status400BadRequest, "Role Id is Empty, it should never happen", null);
            var role = await _roleManager.FindByIdAsync(roleToEdit.Id.ToString());
            if (role.Name.EqualsIgnoreCase("Admin") && !role.Name.EqualsIgnoreCase(roleToEdit.Name))
                return new ApiResponse<AdminEditRoleVM>(StatusCodeType.Status400BadRequest, $"You can't change \"{role.Name}\" Role Name", null);
            if (role.Name.EqualsIgnoreCase("Admin") && !authUser.UserName.EqAnyIgnoreCase(roleToEdit.UserNames))
                return new ApiResponse<AdminEditRoleVM>(StatusCodeType.Status400BadRequest, $"You can't remove \"{role.Name}\" Role from yourself", null);

            _mapper.Map(roleToEdit, role);

            var updateRoleResp = await _roleManager.UpdateAsync(role);
            if (!updateRoleResp.Succeeded)
                return new ApiResponse<AdminEditRoleVM>(StatusCodeType.Status400BadRequest, $"Editing Role \"{roleToEdit.Name}\" Failed. ({updateRoleResp.FirstError()})", null);

            var users = await _userManager.Users.ToListAsync();
            foreach (var user in users)
            {
                if (user.UserName.In(roleToEdit.UserNames) && !await _userManager.IsInRoleAsync(user, roleToEdit.Name))
                {
                    var addUserToRoleResp = await _userManager.AddToRoleAsync(user, roleToEdit.Name);
                    if (!addUserToRoleResp.Succeeded)
                        return new ApiResponse<AdminEditRoleVM>(StatusCodeType.Status400BadRequest, $"Adding Role \"{roleToEdit.Name}\" to User \"{user.UserName}\" Failed. ({updateRoleResp.FirstError()})", null);
                }

                if (!user.UserName.In(roleToEdit.UserNames) && await _userManager.IsInRoleAsync(user, roleToEdit.Name))
                {
                    var removeUserfromRoleResp = await _userManager.RemoveFromRoleAsync(user, roleToEdit.Name);
                    if (!removeUserfromRoleResp.Succeeded)
                        return new ApiResponse<AdminEditRoleVM>(StatusCodeType.Status400BadRequest, $"Removing Role \"{roleToEdit.Name}\" from User \"{user.UserName}\" Failed. ({updateRoleResp.FirstError()})", null);
                }
            }

            return new ApiResponse<AdminEditRoleVM>(StatusCodeType.Status201Created, $"Successfully Updated Role: \"{roleToEdit.Name}\"", null, roleToEdit);
        }

        public async Task<ApiResponse<FindRoleVM>> FindRoleByIdAsync(Guid id)
        {
            var role = await _db.Roles.SingleOrDefaultAsync(u => u.Id == id);
            if (role == null)
                return new ApiResponse<FindRoleVM>(StatusCodeType.Status404NotFound, "There is no Role with the given Id", null);

            var foundRole = _mapper.Map(role, new FindRoleVM());
            foundRole.UserNames = await (
                from ur in _db.UserRoles
                join u in _db.Users on ur.UserId equals u.Id
                join r in _db.Roles on ur.RoleId equals r.Id
                where r.Id == id
                select u.UserName).ToListAsync(); // this is way clearer with query approach and not a method chain

            return new ApiResponse<FindRoleVM>(StatusCodeType.Status200OK, "Finding Role by Id has been Successful", null, foundRole);
        }

        public ApiResponse<FindClaimVM> FindClaimByName(string claimName)
        {
            var claim = (
                from uc in _db.UserClaims.ToList()
                group uc by uc.ClaimType into claimsByType
                where claimsByType.Key.EqualsIgnoreCase(claimName)
                select new FindClaimVM
                {
                    Name = claimsByType.Key,
                    Values = (
                        from cbt in claimsByType
                        group cbt by cbt.ClaimValue into claimByTypeByValue
                        select new FindClaimValueVM
                        {
                            Value = claimByTypeByValue.Key,
                            UserNames = (
                                from cbtbv in claimByTypeByValue
                                join u in _db.Users.ToList() on cbtbv.UserId equals u.Id
                                select u.UserName).ToList()
                        }).ToList()
                }).SingleOrDefault();

            if (claim != null)
                claim.OriginalName = claim.Name; // for 'NotInUse' validation attribute compatibility

            return claim == null
                ? new ApiResponse<FindClaimVM>(StatusCodeType.Status404NotFound, "There is no Claim with the given Name", null)
                : new ApiResponse<FindClaimVM>(StatusCodeType.Status200OK, "Claim Found", null, claim);
        }

        public async Task<ApiResponse<FindClaimVM>> FindClaimByNameAsync(string claimName)
        {
            var claim = (
                from uc in await _db.UserClaims.ToListAsync()
                group uc by uc.ClaimType into claimsByType
                where claimsByType.Key.EqualsIgnoreCase(claimName)
                select new FindClaimVM
                {
                    Name = claimsByType.Key,
                    Values = (
                        from cbt in claimsByType
                        group cbt by cbt.ClaimValue into claimByTypeByValue
                        select new FindClaimValueVM
                        {
                            Value = claimByTypeByValue.Key,
                            UserNames = (
                                from cbtbv in claimByTypeByValue
                                join u in _db.Users.ToList() on cbtbv.UserId equals u.Id
                                select u.UserName).ToList()
                        }).ToList()
                }).SingleOrDefault();

            if (claim != null)
                claim.OriginalName = claim.Name; // for 'NotInUse' validation attribute compatibility

            return claim == null
                ? new ApiResponse<FindClaimVM>(StatusCodeType.Status404NotFound, "There is no Claim with the given Name", null)
                : new ApiResponse<FindClaimVM>(StatusCodeType.Status200OK, "Claim Found", null, claim);
        }

        public async Task<ApiResponse<bool>> HasClaimAsync(FindUserVM user, string claimName)
        {
            var hasClaim = await _db.UserClaims.Join(_db.Users, uc => uc.UserId, u => u.Id, (uc, u) => new { uc, u })
                .AnyAsync(ucu => ucu.uc.ClaimType.ToLower() == claimName.ToLower() && ucu.u.UserName.ToLower() == user.UserName);
            return new ApiResponse<bool>(StatusCodeType.Status200OK, "Checking if User Has a Claim Succeeded", null, hasClaim);
        }

        public async Task<ApiResponse<AdminEditClaimVM>> DeleteClaimAsync(AuthenticateUserVM authUser, AdminEditClaimVM claimToDelete)
        {
            if (authUser == null || !authUser.HasAuthenticationStatus(AuthStatus.Authenticated) || !authUser.HasRole("Admin"))
                return new ApiResponse<AdminEditClaimVM>(StatusCodeType.Status401Unauthorized, "You are not Authorized to Delete Claims", null);
            if (claimToDelete.Name.IsNullOrWhiteSpace())
                return new ApiResponse<AdminEditClaimVM>(StatusCodeType.Status400BadRequest, "Name for the Claim was not supplied, as it is done automatically it should never happen", null);
            var claimResp = await FindClaimByNameAsync(claimToDelete.Name);
            if (claimResp.IsError)
                return new ApiResponse<AdminEditClaimVM>(StatusCodeType.Status400BadRequest, $"Claim \"{claimToDelete.Name}\" was not found, it should never happen", null);

            _db.UserClaims.RemoveBy(c => c.ClaimType.ToLower() == claimToDelete.Name.ToLower());
            await _db.SaveChangesAsync();

            return new ApiResponse<AdminEditClaimVM>(StatusCodeType.Status201Created, $"Successfully Deleted Claim \"{claimToDelete.Name}\"", null, claimToDelete);
        }

        public async Task<ApiResponse<AdminEditClaimVM>> AddClaimAsync(AuthenticateUserVM authUser, AdminEditClaimVM claimToAdd)
        {
            if (authUser == null || !authUser.HasAuthenticationStatus(AuthStatus.Authenticated) || !authUser.HasRole("Admin"))
                return new ApiResponse<AdminEditClaimVM>(StatusCodeType.Status401Unauthorized, "You are not Authorized to Add Claims", null);
            if (claimToAdd.Name.IsNullOrWhiteSpace())
                return new ApiResponse<AdminEditClaimVM>(StatusCodeType.Status400BadRequest, "Claim Name cannot be Empty", new[] { new KeyValuePair<string, string>("Name", "You need to provide Claim Name") }.ToLookup());
            var claimsResp = await GetClaimsAsync(authUser);
            if (claimsResp.IsError)
                return new ApiResponse<AdminEditClaimVM>(StatusCodeType.Status400BadRequest, "Unable to retrieve other Claims", null);
            var otherClaims = claimsResp.Result.Where(c => !c.Name.EqualsIgnoreCase(claimToAdd.OriginalName));
            if (claimToAdd.Name.EqAnyIgnoreCase(otherClaims.Select(c => c.Name)))
                return new ApiResponse<AdminEditClaimVM>(StatusCodeType.Status400BadRequest, "Claim Name cannot be a Duplicate", new[] { new KeyValuePair<string, string>("Name", "Claim Name is a Duplicate") }.ToLookup());
            if (!claimToAdd.GetUserNames().Any())
                return new ApiResponse<AdminEditClaimVM>(StatusCodeType.Status400BadRequest, "You need to choose at least one User because Claims exist soloely in the context of users", null);

            foreach (var claimVal in claimToAdd.Values) // claims have no table, they exist in thew context of users only so if sb removes a claim from all users, the claim is no longer stored anywhere
            {
                foreach (var userName in claimVal.UserNames)
                {
                    var user = await _userManager.FindByNameAsync(userName);
                    if (user is null)
                        return new ApiResponse<AdminEditClaimVM>(StatusCodeType.Status400BadRequest, $"Adding Claim \"{claimToAdd.Name}\" to User \"{userName}\" Failed, there is no such User", null);

                    var hasClaimResp = await HasClaimAsync(_mapper.Map(user, new FindUserVM()), claimToAdd.Name);
                    if (hasClaimResp.IsError)
                        return new ApiResponse<AdminEditClaimVM>(StatusCodeType.Status400BadRequest, $"Checking Claim \"{claimToAdd.Name}\" existence for User \"{userName}\" Failed", null);

                    var hasClaim = hasClaimResp.Result;
                    if (!hasClaim)
                    {
                        var addClaimToUserResp = await _userManager.AddClaimAsync(user, new Claim(claimToAdd.Name, claimVal.Value));
                        if (!addClaimToUserResp.Succeeded)
                            return new ApiResponse<AdminEditClaimVM>(StatusCodeType.Status400BadRequest, $"Adding Claim \"{claimToAdd.Name}\" to User \"{userName}\" Failed. ({addClaimToUserResp.FirstError()})", null);
                    }
                }
            }

            return new ApiResponse<AdminEditClaimVM>(StatusCodeType.Status201Created, $"Successfully Added Claim: \"{claimToAdd.Name}\"", null, claimToAdd);
        }

        public async Task<ApiResponse<FindUserVM>> FindUserByIdAsync(Guid id)
        {
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return new ApiResponse<FindUserVM>(StatusCodeType.Status404NotFound, "There is no User with the given Id", null);

            var foundUser = _mapper.Map(user, new FindUserVM());
            foundUser.Roles = (await _userManager.GetRolesAsync(user)).Select(r => FindRoleByName(r).Result).ToList();
            foundUser.Claims = (await _userManager.GetClaimsAsync(user)).Select(c => FindClaimByName(c.Type).Result).Where(c => !c.Name.EqualsIgnoreCase("Email")).ToList();

            return new ApiResponse<FindUserVM>(StatusCodeType.Status200OK, "Finding User by Id has been Successful", null, foundUser);
        }

        public async Task<ApiResponse<AdminEditClaimVM>> EditClaimAsync(AuthenticateUserVM authUser, AdminEditClaimVM claimToEdit)
        {
            if (authUser == null || !authUser.HasAuthenticationStatus(AuthStatus.Authenticated) || !authUser.HasRole("Admin"))
                return new ApiResponse<AdminEditClaimVM>(StatusCodeType.Status401Unauthorized, "You are not Authorized to Edit Claims", null);
            if (claimToEdit.Name.IsNullOrWhiteSpace())
                return new ApiResponse<AdminEditClaimVM>(StatusCodeType.Status400BadRequest, "Claim Name cannot be Empty", new[] { new KeyValuePair<string, string>("Name", "You need to provide Claim Name") }.ToLookup());
            var claimsResp = await GetClaimsAsync(authUser);
            if (claimsResp.IsError)
                return new ApiResponse<AdminEditClaimVM>(StatusCodeType.Status400BadRequest, "Unable to retrieve other Claims", null);
            var otherClaims = claimsResp.Result.Where(c => !c.Name.EqualsIgnoreCase(claimToEdit.OriginalName));
            if (claimToEdit.Name.EqAnyIgnoreCase(otherClaims.Select(c => c.Name)))
                return new ApiResponse<AdminEditClaimVM>(StatusCodeType.Status400BadRequest, "Claim Name cannot be a Duplicate", new[] { new KeyValuePair<string, string>("Name", "Claim Name is a Duplicate") }.ToLookup());
            if (!claimToEdit.GetUserNames().Any())
                return new ApiResponse<AdminEditClaimVM>(StatusCodeType.Status400BadRequest, "You need to choose at least one User because Claims exist soloely in the context of users", null);

            _db.UserClaims.RemoveBy(c => c.ClaimType.ToLower() == claimToEdit.OriginalName.ToLower()); // as much as I'd love to add 'EqualsInvariantIgnoreCase' in all these 'Queryable' backed places, I can't :/.
            await _db.SaveChangesAsync();
            foreach (var claimVal in claimToEdit.Values) // claims have no table, they exist in thew context of users only so if sb removes a claim from all users, the claim is no longer stored anywhere
            {
                foreach (var userName in claimVal.UserNames)
                {
                    var user = await _userManager.FindByNameAsync(userName);
                    if (user == null)
                        return new ApiResponse<AdminEditClaimVM>(StatusCodeType.Status400BadRequest, $"Editing Claim \"{claimToEdit.Name}\" for User \"{userName}\" Failed, there is no such User", null);
                    var hasClaimResp = await HasClaimAsync(_mapper.Map(user, new FindUserVM()), claimToEdit.Name);
                    if (hasClaimResp.IsError)
                        return new ApiResponse<AdminEditClaimVM>(StatusCodeType.Status400BadRequest, $"Checking Claim \"{claimToEdit.Name}\" existence for User \"{userName}\" Failed", null);
                    var hasClaim = hasClaimResp.Result;

                    if (!hasClaim)
                    {
                        var addClaimToUserResp = await _userManager.AddClaimAsync(user, new Claim(claimToEdit.Name, claimVal.Value));
                        if (!addClaimToUserResp.Succeeded)
                            return new ApiResponse<AdminEditClaimVM>(StatusCodeType.Status400BadRequest, $"Editing Claim \"{claimToEdit.Name}\" for User \"{userName}\" Failed. ({addClaimToUserResp.FirstError()})", null);
                    }
                }
            }

            return new ApiResponse<AdminEditClaimVM>(StatusCodeType.Status201Created, $"Successfully Edited Claim: \"{claimToEdit.Name}\"", null, claimToEdit);
        }
    }
}
