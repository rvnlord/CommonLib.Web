﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Source.Models;
using CommonLib.Web.Source.Common.Converters;
using CommonLib.Web.Source.DbContext;
using CommonLib.Web.Source.DbContext.Models.Account;
using CommonLib.Web.Source.Security;
using CommonLib.Web.Source.Services.Account.Interfaces;
using CommonLib.Web.Source.Validators.Account;
using CommonLib.Web.Source.ViewModels.Account;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nethereum.Signer;
using Nethereum.Util;

namespace CommonLib.Web.Source.Services.Account
{
    public class AccountManager : IAccountManager
    {
        private readonly UserManager<DbUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly SignInManager<DbUser> _signInManager;
        private readonly AccountDbContext _db;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _http;
        private readonly IPasswordHasher<DbUser> _passwordHasher;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly CustomPasswordResetTokenProvider<DbUser> _passwordResetTokenProvider;

        public AccountManager(UserManager<DbUser> userManager,
            SignInManager<DbUser> signInManager,
            IEmailSender emailSender,
            AccountDbContext db,
            IMapper autoMapper,
            IHttpContextAccessor http,
            IPasswordHasher<DbUser> passwordHasher,
            RoleManager<IdentityRole<Guid>> roleManager,
            CustomPasswordResetTokenProvider<DbUser> passwordResetTokenProvider)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _db = db;
            _mapper = autoMapper;
            _http = http;
            _passwordHasher = passwordHasher;
            _roleManager = roleManager;
            _passwordResetTokenProvider = passwordResetTokenProvider;
        }

        private async Task<ApiResponse<FindUserVM>> FindUserAsync(FindUserVM userToFind, bool includeEmailClaim = false)
        {
            DbUser user;
            if (userToFind.Id != Guid.Empty)
                user = await _db.Users.SingleOrDefaultAsync(u => u.Id == userToFind.Id);
            else if (!userToFind.UserName.IsNullOrWhiteSpace())
                user = await _db.Users.SingleOrDefaultAsync(u => u.UserName.ToLower() == userToFind.UserName.ToLower());
            else if (!userToFind.Email.IsNullOrWhiteSpace())
                user = await _db.Users.SingleOrDefaultAsync(u => u.Email.ToLower() == userToFind.Email.ToLower());
            else if (!userToFind.Email.IsNullOrWhiteSpace())
                user = await _db.Users.SingleOrDefaultAsync(u => u.EmailActivationToken.ToLower() == userToFind.EmailActivationToken.ToLower());
            else
                return new ApiResponse<FindUserVM>(StatusCodeType.Status406NotAcceptable, "No property specified can be used to uniquely identify a correct user");

            if (user is null)
                return new ApiResponse<FindUserVM>(StatusCodeType.Status200OK, "There is no User with the given Name");

            var foundUser = _mapper.Map(user, new FindUserVM());
            foundUser.Roles = await _userManager.GetRolesAsync(user).SelectAsync(async r => (await FindRoleByNameAsync(r)).Result).OrderByAsync(r => r.Name).ToListAsync();
            foundUser.Claims = await _userManager.GetClaimsAsync(user).SelectAsync(async c => (await FindClaimByNameAsync(c.Type)).Result).WhereAsync(c => includeEmailClaim || !c.Name.EqualsIgnoreCase("Email")).OrderByAsync(r => r.Name).ToListAsync();

            return new ApiResponse<FindUserVM>(StatusCodeType.Status200OK, "Finding User by Name has been Successful", foundUser);
        }

        public async Task<ApiResponse<FindRoleVM>> FindRoleByNameAsync(string roleName)
        {
            var role = await _db.Roles.SingleOrDefaultAsync(r => r.Name.ToLower() == roleName.ToLower());
            if (role is null)
                return new ApiResponse<FindRoleVM>(StatusCodeType.Status200OK, "There is no Role with the given Name");

            var foundRole = _mapper.Map(role, new FindRoleVM());
            return new ApiResponse<FindRoleVM>(StatusCodeType.Status200OK, "Role Found", null, foundRole);
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
                    OriginalName = claimsByType.Key,
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

            if (claim is not null)
                claim.OriginalName = claim.Name; // for 'NotInUse' validation attribute compatibility

            return claim is null
                ? new ApiResponse<FindClaimVM>(StatusCodeType.Status200OK, "There is no Claim with the given Name")
                : new ApiResponse<FindClaimVM>(StatusCodeType.Status200OK, "Claim Found", claim);
        }

        public Task<ApiResponse<FindUserVM>> FindUserByIdAsync(Guid id, bool includeEmailClaim = false) => FindUserAsync(FindUserVM.FromId(id), includeEmailClaim);
        public Task<ApiResponse<FindUserVM>> FindUserByNameAsync(string name) => FindUserAsync(FindUserVM.FromUserName(name));
        public Task<ApiResponse<FindUserVM>> FindUserByEmailAsync(string email) => FindUserAsync(FindUserVM.FromEmail(email));
        public Task<ApiResponse<FindUserVM>> FindUserByConfirmationCodeAsync(string confirmationCode) => FindUserAsync(FindUserVM.FromEmailActivationToken(confirmationCode));

        public async Task<ApiResponse<FileData>> GetUserAvatarByNameAsync(string name)
        {
            var user = await IEnumerableExtensions.SingleOrDefaultAsync(_db.Users.Include(u => u.Avatar), u => u.UserName.ToLower() == name.ToLower());
            if (user is null)
                return new ApiResponse<FileData>(StatusCodeType.Status200OK, "There is no User with the given Name");

            var avatar = user.Avatar?.ToFileData();
            return new ApiResponse<FileData>(StatusCodeType.Status200OK, "Avatar retrieved Successfully", avatar);
        }

        public async Task<ApiResponse<FileDataList>> FindAvatarsInUseAsync(bool includeData)
        {
            var files = (await _db.Files.WhereAsync(f => f.UserHavingFileAsAvatarId != null)).ToFileDataList(false);
            return new ApiResponse<FileDataList>(StatusCodeType.Status200OK, "Getting Avatars in Use was Successful", null, files);
        }

        public async Task<ApiResponse<bool>> CheckUserManagerComplianceAsync(string userPropertyName, string userPropertyDisplayName, string userPropertyValue)
        {
            await Task.CompletedTask;

            if (userPropertyValue == null)
                return new ApiResponse<bool>(StatusCodeType.Status200OK, $"{userPropertyDisplayName} is Empty, falling through to other attributes", null, true);

            if (userPropertyName.ContainsIgnoreCase("UserName"))
            {
                if (!userPropertyValue.All(c => c.In(_userManager.Options.User.AllowedUserNameCharacters))) // we are skipping unique email check here because we are already checking email in other attrbiute and because email is not part of username property despite the fact that usermanager have email option under DbUser category for some reason
                    return new ApiResponse<bool>(StatusCodeType.Status400BadRequest, $"{userPropertyDisplayName} contains disallowed characters, allowed characters are: [{_userManager.Options.User.AllowedUserNameCharacters.Select(c => $"\'{c}\'").JoinAsString(", ")}]", null, false);
            }

            if (userPropertyName.ContainsIgnoreCase("Password"))
            {
                if (_userManager.Options.Password.RequireDigit && !userPropertyValue.Any(char.IsDigit))
                    return new ApiResponse<bool>(StatusCodeType.Status400BadRequest, $"{userPropertyDisplayName} need to contain at least one digit", null, false);
                if (_userManager.Options.Password.RequireLowercase && !userPropertyValue.Any(char.IsLower))
                    return new ApiResponse<bool>(StatusCodeType.Status400BadRequest, $"{userPropertyDisplayName} need to contain at least one lower case character", null, false);
                if (_userManager.Options.Password.RequireUppercase && !userPropertyValue.Any(char.IsUpper))
                    return new ApiResponse<bool>(StatusCodeType.Status400BadRequest, $"{userPropertyDisplayName} need to contain at least one upper case character", null, false);
                if (_userManager.Options.Password.RequireNonAlphanumeric && userPropertyValue.All(char.IsLetterOrDigit))
                    return new ApiResponse<bool>(StatusCodeType.Status400BadRequest, $"{userPropertyDisplayName} need to contain at least one non-alphanumeric character", null, false);
                if (_userManager.Options.Password.RequiredLength > userPropertyValue.Length)
                    return new ApiResponse<bool>(StatusCodeType.Status400BadRequest, $"{userPropertyDisplayName} has to be at least {_userManager.Options.Password.RequiredLength} characters long", null, false);
                if (_userManager.Options.Password.RequiredUniqueChars > userPropertyValue.Distinct().Count())
                    return new ApiResponse<bool>(StatusCodeType.Status400BadRequest, $"{userPropertyDisplayName} has to contain at least {_userManager.Options.Password.RequiredUniqueChars} unique characters", null, false);
            }

            return new ApiResponse<bool>(StatusCodeType.Status200OK, $"{userPropertyDisplayName} is User Manager Compliant", null, true);
        }

        public async Task<ApiResponse<AuthenticateUserVM>> GetAuthenticatedUserAsync(HttpContext http, ClaimsPrincipal principal, AuthenticateUserVM userToAuthenticate)
        {
            userToAuthenticate.AuthenticationStatus = AuthStatus.NotAuthenticated;
            var contextPrincipal = http != null ? (await http.AuthenticateAsync(IdentityConstants.ApplicationScheme))?.Principal : null;
            var principals = new[] { principal, contextPrincipal };
            var claimsPrincipal = principals.FirstOrDefault(p => p?.Identity?.Name != null && p.Identity.IsAuthenticated);

            if (userToAuthenticate.Ticket.IsNullOrWhiteSpace())
            {
                await _signInManager.SignOutAsync();
                return new ApiResponse<AuthenticateUserVM>(StatusCodeType.Status200OK, "User is not Authenticated", null, userToAuthenticate);
            }

            var key = (await IEnumerableExtensions.SingleOrDefaultAsync(_db.CryptographyKeys, k => k.Name == "LoginTicket"))?.Value?.Base58ToByteArray();
            var decryptedTicket = userToAuthenticate.Ticket.Base58ToByteArray().DecryptCamellia(key).ToBase58String().Base58ToUTF8().Split("|");
            var timeStamp = decryptedTicket[0].ToLong().UnixTimeStampToDateTime();
            var id = decryptedTicket[1];
            var passwordHash = decryptedTicket[2].NullifyIf(ph => ph.IsNullOrWhiteSpace()); // we need to nullify the empty string that comes from a decrypted ticket because otherwise we would get passwordHash ("") == user.PasswordHash (null) = false
            var rememberMe = decryptedTicket[3].ToBool();

            DbUser user = null;
            if (claimsPrincipal?.Identity?.Name != null)
                user = await _userManager.FindByNameAsync(claimsPrincipal.Identity.Name);
            if (user == null)
            {
                user = await _userManager.FindByIdAsync(id);
                if (user == null || !id.EqualsInvariant(user.Id.ToString()) || !passwordHash.EqualsInvariant(user.PasswordHash) || DateTimeOffset.UtcNow - timeStamp >= TimeSpan.FromDays(365))
                {
                    await _signInManager.SignOutAsync();
                    return new ApiResponse<AuthenticateUserVM>(StatusCodeType.Status200OK, "User is not Authenticated", null, userToAuthenticate);
                }

                await _signInManager.SignInAsync(user, true);
            }

            _mapper.Map(user, userToAuthenticate);
            userToAuthenticate.RememberMe = rememberMe;
            userToAuthenticate.HasPassword = user.PasswordHash != null;
            userToAuthenticate.AuthenticationStatus = AuthStatus.Authenticated;
            userToAuthenticate.LoginTimestamp = timeStamp.ToExtendedTime();
            userToAuthenticate.Roles = (await _userManager.GetRolesAsync(user)).Select(r => new FindRoleVM { Name = r }).ToList();
            userToAuthenticate.Claims = (await _userManager.GetClaimsAsync(user)).Select(c => new FindClaimVM { Name = c.Type }).Where(c => !c.Name.EqualsIgnoreCase("Email")).ToList();
            return new ApiResponse<AuthenticateUserVM>(StatusCodeType.Status200OK, "Getting Authenticated User was Successful", null, userToAuthenticate);
        }

        public async Task<ApiResponse<RegisterUserVM>> RegisterAsync(RegisterUserVM userToRegister)
        {
            var user = new DbUser { UserName = userToRegister.UserName, Email = userToRegister.Email };
            var result = userToRegister.Password is not null ? await _userManager.CreateAsync(user, userToRegister.Password) : await _userManager.CreateAsync(user);
            if (!result.Succeeded) // new List<IdentityError> { new() { Code = "Password", Description = "Password Error TEST" } }
            {
                if (result.Errors.Any(e => e.Code.EqualsIgnoreCase("DuplicateUserName")))
                {
                    var i = 1;
                    while (await _db.Users.SingleOrDefaultAsync(u => u.UserName.ToLower() == userToRegister.UserName.ToLower()) is not null)
                        i++;

                    userToRegister.UserName += $"_{i}";
                    user.UserName = userToRegister.UserName;

                    result = userToRegister.Password is not null ? await _userManager.CreateAsync(user, userToRegister.Password) : await _userManager.CreateAsync(user);
                }

                if (!result.Succeeded)
                {
                    var errors = result.Errors.ToLookup(userToRegister.GetPropertyNames());
                    return new ApiResponse<RegisterUserVM>(StatusCodeType.Status401Unauthorized, result.Errors.First().Description.Replace("'", "\""), errors, null);
                }
            }

            if (user.Email is not null) // would be null when registering with wallet provider
                await _userManager.AddClaimAsync(user, new Claim("Email", user.Email));

            if (!await _roleManager.RoleExistsAsync("Admin"))
                await _roleManager.CreateAsync(new IdentityRole<Guid>("Admin"));
            if (!await _roleManager.RoleExistsAsync("User"))
                await _roleManager.CreateAsync(new IdentityRole<Guid>("User"));

            if (!_db.Users.Any())
                await _userManager.AddToRoleAsync(user, "Admin");
            await _userManager.AddToRoleAsync(user, "User");

            if (_userManager.Options.SignIn.RequireConfirmedEmail && user.Email is not null) // if registering from wallet provider email would be null
            {
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var deployingEmailResponse = await _emailSender.SendConfirmationEmailAsync(userToRegister.Email, code, userToRegister.ReturnUrl);
                if (deployingEmailResponse.IsError)
                {
                    userToRegister.ReturnUrl = $"/Account/ResendEmailConfirmation/?email={userToRegister.Email.UTF8ToBase58()}&returnUrl={userToRegister.ReturnUrl.UTF8ToBase58()}";
                    return new ApiResponse<RegisterUserVM>(StatusCodeType.Status500InternalServerError, "Registration had been Successful, but the email wasn't sent. Try again later.", null, userToRegister, deployingEmailResponse.ResponseException);
                }

                _mapper.Map(user, userToRegister);
                userToRegister.ReturnUrl = $"/Account/ConfirmEmail?email={userToRegister.Email.UTF8ToBase58()}&returnUrl={userToRegister.ReturnUrl.UTF8ToBase58()}";
                return new ApiResponse<RegisterUserVM>(StatusCodeType.Status201Created, $"Registration for User \"{userToRegister.UserName}\" has been successful, activation email has been deployed to: \"{userToRegister.Email}\"", null, _mapper.Map(userToRegister, new RegisterUserVM()));
            }

            _mapper.Map(user, userToRegister);
            userToRegister.ReturnUrl = $"/Account/Login?returnUrl={userToRegister.ReturnUrl.UTF8ToBase58()}";
            userToRegister.Ticket = await GenerateLoginTicketAsync(user.Id, user.PasswordHash, false);
            await _signInManager.SignInAsync(user, false);
            return new ApiResponse<RegisterUserVM>(StatusCodeType.Status201Created, $"Registration for User \"{userToRegister.UserName}\" has been successful, you are now logged in", null, userToRegister);
        }

        public async Task<ApiResponse<ConfirmUserVM>> ConfirmEmailAsync(ConfirmUserVM userToConfirm)
        {
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Email.ToLower() == userToConfirm.Email.ToLower());
            if (user == null)
                return new ApiResponse<ConfirmUserVM>(StatusCodeType.Status401Unauthorized, "There is no User with this Email to Confirm", new[] { new KeyValuePair<string, string>("Email", "No such email") }.ToLookup());

            var isAlreadyConfirmed = await _userManager.IsEmailConfirmedAsync(user);
            if (isAlreadyConfirmed)
                return new ApiResponse<ConfirmUserVM>(StatusCodeType.Status401Unauthorized, "Email is confirmed", new[] { new KeyValuePair<string, string>("Email", "Email has already been confirmed") }.ToLookup());

            if (!userToConfirm.ConfirmationCode.IsBase58())
                return new ApiResponse<ConfirmUserVM>(StatusCodeType.Status400BadRequest, "Email confirmation failed", new[] { new KeyValuePair<string, string>("ConfirmationCode", "Confirmation Code is Invalid") }.ToLookup());

            var confirmationResult = await _userManager.ConfirmEmailAsync(user, userToConfirm.ConfirmationCode);
            if (!confirmationResult.Succeeded)
            {
                var errors = confirmationResult.Errors.ToSinglePropertyLookup("ConfirmationCode");
                return new ApiResponse<ConfirmUserVM>(StatusCodeType.Status401Unauthorized, "Email confirmation failed", errors);
            }

            _mapper.Map(user, userToConfirm); // account for null in source if destination is not null in MappingProfile

            return new ApiResponse<ConfirmUserVM>(StatusCodeType.Status200OK, "Email has been Confirmed Successfully", null, userToConfirm);
        }

        public async Task<string> GenerateLoginTicketAsync(Guid id, string passwordHash, bool rememberMe)
        {
            var key = (await _db.CryptographyKeys.AsNoTracking().SingleOrDefaultAsync(k => k.Name.ToLower() == "LoginTicket"))?.Value;
            if (key == null)
            {
                key = CryptoUtils.GenerateCamelliaKey().ToBase58String();
                await _db.CryptographyKeys.AddAsync(new DbCryptographyKey { Name = "LoginTicket", Value = key });
                await _db.SaveChangesAsync();
            }

            return $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}|{id}|{passwordHash}|{Convert.ToInt32(rememberMe)}".UTF8ToByteArray()
                .EncryptCamellia(key.Base58ToByteArray()).ToBase58String();
        }

        public async Task<ApiResponse<ResendConfirmationEmailUserVM>> ResendConfirmationEmailAsync(ResendConfirmationEmailUserVM userToResendConfirmationEmail)
        {
            var successMessage = $"Confirmation email has been sent to: \"{userToResendConfirmationEmail.Email}\" if there is an account associated with it";
            userToResendConfirmationEmail.ReturnUrl += $"?email={userToResendConfirmationEmail.Email}";
            var emailReturnUrl = PathUtils.Combine(PathSeparator.FSlash, ConfigUtils.FrontendBaseUrl, "Account/Login");
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Email.ToLower() == userToResendConfirmationEmail.Email.ToLower());
            if (user == null)
                return new ApiResponse<ResendConfirmationEmailUserVM>(StatusCodeType.Status200OK, successMessage, null, userToResendConfirmationEmail);

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                userToResendConfirmationEmail.ReturnUrl = emailReturnUrl;
                return new ApiResponse<ResendConfirmationEmailUserVM>(StatusCodeType.Status200OK, $"Email \"{userToResendConfirmationEmail.Email}\" has already been confirmed", null, userToResendConfirmationEmail);
            }

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var resendingConfirmationEmailResponse = await _emailSender.SendConfirmationEmailAsync(userToResendConfirmationEmail.Email, code, emailReturnUrl);
            if (resendingConfirmationEmailResponse.IsError)
                return new ApiResponse<ResendConfirmationEmailUserVM>(StatusCodeType.Status500InternalServerError, "Can't resend Confirmation Email. Please try again later.", null, null, resendingConfirmationEmailResponse.ResponseException);

            return new ApiResponse<ResendConfirmationEmailUserVM>(StatusCodeType.Status200OK, successMessage, null, _mapper.Map(user, userToResendConfirmationEmail));
        }

        public async Task<ApiResponse<LoginUserVM>> LoginAsync(LoginUserVM userToLogin)
        {
            if (userToLogin.UserName.IsNullOrWhiteSpace())
                return new ApiResponse<LoginUserVM>(StatusCodeType.Status401Unauthorized, "User Name can't be empty", new[] { new KeyValuePair<string, string>("UserName", "User Name is required") }.ToLookup());

            var user = await _userManager.FindByNameAsync(userToLogin.UserName);
            if (user is null)
                return new ApiResponse<LoginUserVM>(StatusCodeType.Status401Unauthorized, "User Name not found, please Register first", new[] { new KeyValuePair<string, string>("UserName", "There is no DbUser with this UserName") }.ToLookup());

            userToLogin.Email = user.Email; // userName if we are sedarching by email but here we are searching for name
            if (!user.EmailConfirmed && await _userManager.CheckPasswordAsync(user, userToLogin.Password) && _userManager.Options.SignIn.RequireConfirmedEmail)
                return new ApiResponse<LoginUserVM>(StatusCodeType.Status401Unauthorized, "Confirm your account by clicking the link in your email first", new[] { new KeyValuePair<string, string>("Email", "Email is not confirmed yet") }.ToLookup());

            var loginResult = await _signInManager.PasswordSignInAsync(userToLogin.UserName, userToLogin.Password, userToLogin.RememberMe, true);
            if (!loginResult.Succeeded)
            {
                var message = string.Empty;
                var property = nameof(userToLogin.Email);
                if (loginResult == SignInResult.Failed)
                {
                    var failedLoginAttempts = await _userManager.GetAccessFailedCountAsync(user);
                    var maxLoginAttempts = _userManager.Options.Lockout.MaxFailedAccessAttempts;
                    property = nameof(userToLogin.Password);
                    if (user.PasswordHash != null)
                        message = $"Incorrect Password ({maxLoginAttempts - failedLoginAttempts} attempts left)";
                    else
                        message = $"You don't have any password set for your Account, Please log in with your External Provider and set one or use Reset Password Form in case you don't have access to your external Account ({maxLoginAttempts - failedLoginAttempts} attempts left)";
                }
                else if (loginResult == SignInResult.NotAllowed)
                    message = "You don't have permission to Sign-In";
                else if (loginResult == SignInResult.LockedOut)
                {
                    var lockoutEndDate = await _userManager.GetLockoutEndDateAsync(user) ?? new DateTimeOffset(); // never null because lockout flag is true at this point
                    var lockoutTimeLeft = lockoutEndDate - DateTime.UtcNow;
                    message = $"Account Locked, too many failed attempts (try again in: {lockoutTimeLeft.Minutes}m {lockoutTimeLeft.Seconds}s)";
                }
                else if (loginResult == SignInResult.TwoFactorRequired)
                    message = "2FA Code hasn't been provided";
                return new ApiResponse<LoginUserVM>(StatusCodeType.Status401Unauthorized, $"Login Failed - {message}", new[] { new KeyValuePair<string, string>(property, message) }.ToLookup());
            }

            await _userManager.ResetAccessFailedCountAsync(user);
            userToLogin.Ticket = await GenerateLoginTicketAsync(user.Id, user.PasswordHash, userToLogin.RememberMe);
            return new ApiResponse<LoginUserVM>(StatusCodeType.Status200OK, $"You are now logged in as \"{userToLogin.UserName}\"", null, _mapper.Map(user, userToLogin));
        }

        public async Task<(AuthenticationProperties authenticationProperties, string schemaName)> ExternalLoginAsync(LoginUserVM userToExternalLogin)
        {
            var schemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
            var scheme = schemes.SingleOrDefault(s => s.Name.EqualsIgnoreCase(userToExternalLogin.ExternalProvider));
            var frontEndBaseurl = ConfigUtils.FrontendBaseUrl;
            if (scheme == null)
                throw new ArgumentNullException(null, $"{frontEndBaseurl}Account/Login?remoteStatus=Error&remoteMessage={"Provider not Found".UTF8ToBase58()}");

            var reqScheme = _http.HttpContext?.Request.Scheme; // we need API url here which is different than the Web App one
            var host = _http.HttpContext?.Request.Host;
            var pathbase = _http.HttpContext?.Request.PathBase;
            var redirectUrl = $"{reqScheme}://{host}{pathbase}/api/account/externallogincallback?returnUrl={userToExternalLogin.ReturnUrl}&user={userToExternalLogin.JsonSerialize().UTF8ToBase58()}";
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(userToExternalLogin.ExternalProvider, redirectUrl);

            return (properties, scheme.Name);
        }

        public async Task<string> ExternalLoginCallbackAsync(string returnUrl, string remoteError)
        {
            var userToExternalLogin = _http.HttpContext?.Request.Query["user"].ToString().Base58ToUTF8().JsonDeserialize().To<LoginUserVM>() ?? throw new NullReferenceException("'userToExternalLogin' was null");
            var decodedReturnUrl = returnUrl.Base58ToUTF8();
            var url = decodedReturnUrl.BeforeFirstOrWhole("?");
            var qs = decodedReturnUrl.QueryStringToDictionary();
            qs["remoteStatus"] = "Error";

            try
            {
                if (remoteError is not null)
                {
                    qs["remoteMessage"] = remoteError.UTF8ToBase58();
                    return $"{url}?{qs.ToQueryString()}";
                }

                var externalLoginInfo = await _signInManager.GetExternalLoginInfoAsync();
                if (externalLoginInfo is null)
                {
                    qs["remoteMessage"] = "Error loading external login information".UTF8ToBase58();
                    return $"{url}?{qs.ToQueryString()}";
                }

                await _signInManager.UpdateExternalAuthenticationTokensAsync(externalLoginInfo); // add ext auth data to the db

                userToExternalLogin.Email = externalLoginInfo.Principal.FindFirstValue(ClaimTypes.Email);
                userToExternalLogin.UserName = externalLoginInfo.Principal.FindFirstValue(ClaimTypes.Name) ?? userToExternalLogin.Email.BeforeFirst("@");
                userToExternalLogin.ExternalProvider = externalLoginInfo.LoginProvider;
                userToExternalLogin.ExternalProviderKey = externalLoginInfo.ProviderKey;
                qs["user"] = userToExternalLogin.JsonSerialize().UTF8ToBase58();
                qs.Remove("remoteStatus");
                return $"{url}?{qs.ToQueryString()}";
            }
            catch (Exception ex)
            {
                qs["remoteMessage"] = $"Retrieving Provider Key Failed - {ex.Message}".UTF8ToBase58();
                return $"{url}?{qs.ToQueryString()}";
            }
        }

        public async Task<ApiResponse<LoginUserVM>> ExternalLoginAuthorizeAsync(LoginUserVM userToExternalLogin)
        {
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Email.ToLower() == userToExternalLogin.Email.ToLower());
            var userAccountNotExist = user is null;
            if (userAccountNotExist)
            {
                var userToRegister = new RegisterUserVM
                {
                    UserName = userToExternalLogin.UserName ?? userToExternalLogin.Email.BeforeFirstOrNull("@"),
                    Email = userToExternalLogin.Email,
                    ReturnUrl = userToExternalLogin.ReturnUrl
                };
                var registerUserResponse = await RegisterAsync(userToRegister);
                if (registerUserResponse.IsError)
                    return new ApiResponse<LoginUserVM>(StatusCodeType.Status401Unauthorized, registerUserResponse.Message);
                user = await _userManager.FindByEmailAsync(userToExternalLogin.Email);
            }

            if (!user.EmailConfirmed && _userManager.Options.SignIn.RequireConfirmedEmail)
            {
                _mapper.Map(user, userToExternalLogin); // to account for isconfirmed
                return userAccountNotExist
                    ? new ApiResponse<LoginUserVM>(StatusCodeType.Status200OK, $"Your account has been successfully created, confirmation email has been sent to: \"{userToExternalLogin.Email}\"", userToExternalLogin)
                    : new ApiResponse<LoginUserVM>(StatusCodeType.Status401Unauthorized, "Please confirm your email first");
            }

            var externalLoginResult = await _signInManager.ExternalLoginSignInAsync(userToExternalLogin.ExternalProvider, userToExternalLogin.ExternalProviderKey, userToExternalLogin.RememberMe, true);
            if (!externalLoginResult.Succeeded)
            {
                var addLoginResult = await _userManager.AddLoginAsync(user, new UserLoginInfo(userToExternalLogin.ExternalProvider, userToExternalLogin.ExternalProviderKey, userToExternalLogin.ExternalProvider));
                if (!addLoginResult.Succeeded)
                {
                    var message = "There was an Unknown Error during External User Login";
                    if (externalLoginResult == SignInResult.NotAllowed)
                        message = "You don't have permission to Sign-In with an External Provider";
                    if (externalLoginResult == SignInResult.LockedOut)
                    {
                        var lockoutEndDate = await _userManager.GetLockoutEndDateAsync(user) ?? new DateTimeOffset(); // never null because lockout flag is true at this point
                        var lockoutTimeLeft = lockoutEndDate - DateTime.UtcNow;
                        message = $"Account Locked, too many failed attempts (try again in: {lockoutTimeLeft.Minutes}m {lockoutTimeLeft.Seconds}s)";
                    }

                    return new ApiResponse<LoginUserVM>(StatusCodeType.Status401Unauthorized, message);
                }

                var secondAttemptExternalLoginResult = await _signInManager.ExternalLoginSignInAsync(userToExternalLogin.ExternalProvider, userToExternalLogin.ExternalProviderKey, userToExternalLogin.RememberMe, true);
                if (!secondAttemptExternalLoginResult.Succeeded)
                    return new ApiResponse<LoginUserVM>(StatusCodeType.Status401Unauthorized, "User didn't have an External Login Account so it was added, but Login Attempt has Failed");
            }

            await _signInManager.SignInAsync(user, userToExternalLogin.RememberMe);
            await _userManager.ResetAccessFailedCountAsync(user);

            _mapper.Map(user, userToExternalLogin);
            userToExternalLogin.Ticket = await GenerateLoginTicketAsync(user.Id, user.PasswordHash, userToExternalLogin.RememberMe);

            //var externalAccessToken = await _userManager.GetAuthenticationTokenAsync(user, "Discord", "access_token");
            //await using var client = new DiscordRestClient();
            //await client.LoginAsync(TokenType.Bearer, externalAccessToken);
            //var guilds = await client.GetGuildSummariesAsync().FlattenAsync().ToListAsync();

            //var request = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/users/@me/guilds");
            //request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", externalAccessToken);
            //var response = await new HttpClient().SendAsync(request);
            //var content = await response.Content.ReadAsStringAsync();

            return new ApiResponse<LoginUserVM>(StatusCodeType.Status200OK, $"You have been successfully logged in with \"{userToExternalLogin.ExternalProvider}\" as: \"{userToExternalLogin.UserName}\"", null, userToExternalLogin);
        }

        public async Task<ApiResponse<LoginUserVM>> WalletLoginAsync(LoginUserVM userToWalletLogin)
        {
            if (userToWalletLogin.WalletAddress.IsNullOrWhiteSpace())
                return new ApiResponse<LoginUserVM>(StatusCodeType.Status401Unauthorized, "Wallet Address can't be empty", new[] { new KeyValuePair<string, string>("WalletAddress", "Wallet Address is required") }.ToLookup());
            if (userToWalletLogin.WalletSignature.IsNullOrWhiteSpace())
                return new ApiResponse<LoginUserVM>(StatusCodeType.Status401Unauthorized, "Wallet Signature can't be empty", new[] { new KeyValuePair<string, string>("WalletSignature", "Wallet Signature is required") }.ToLookup());
            
            if (userToWalletLogin.WalletProvider.EqualsIgnoreCase("Metamask"))
            {
                if (!AddressUtil.Current.IsValidEthereumAddressHexFormat(userToWalletLogin.WalletAddress))
                    return new ApiResponse<LoginUserVM>(StatusCodeType.Status401Unauthorized, "Wallet Address is invalid", new[] { new KeyValuePair<string, string>("WalletAddress", "Wallet Address is invalid") }.ToLookup());

                var message = $"Proving ownership of wallet: \"{userToWalletLogin.WalletAddress}\"";
                var address = new EthereumMessageSigner().EcRecover(message.UTF8ToByteArray(), userToWalletLogin.WalletSignature);
                var isSignatureCorrect = address.EqualsIgnoreCase(userToWalletLogin.WalletAddress); // address returned by Nethereum from ECRecover() is actually upper case for some reason
                if (!isSignatureCorrect)
                    return new ApiResponse<LoginUserVM>(StatusCodeType.Status401Unauthorized, "Wallet Signature is incorrect", new[] { new KeyValuePair<string, string>("WalletSignature", "Wallet Signature is incorrect") }.ToLookup());
            }
            else
                return new ApiResponse<LoginUserVM>(StatusCodeType.Status401Unauthorized, "Wallet Provider not supported", new[] { new KeyValuePair<string, string>("WalletProvider", "Wallet Provider not supported") }.ToLookup());

            var user = _db.Users.Include(u => u.Wallets).SingleOrDefault(u => u.Wallets.Any(w => w.Address == userToWalletLogin.WalletAddress && w.Provider.ToLower() == userToWalletLogin.WalletProvider.ToLower()));
            var userAccountNotExist = user is null;
            if (userAccountNotExist)
            {
                var userToRegister = new RegisterUserVM
                {
                    UserName = $"{userToWalletLogin.WalletAddress.Take(6)}...{userToWalletLogin.WalletAddress.TakeLast(4)}",
                    ReturnUrl = userToWalletLogin.ReturnUrl
                };
                var registerUserResponse = await RegisterAsync(userToRegister);
                if (registerUserResponse.IsError)
                    return new ApiResponse<LoginUserVM>(StatusCodeType.Status401Unauthorized, registerUserResponse.Message);

                await _db.Wallets.AddAsync(new DbWallet
                {
                    Provider = userToWalletLogin.WalletProvider,
                    Address = userToWalletLogin.WalletAddress,
                    UserId = registerUserResponse.Result.Id,
                });
                await _db.SaveChangesAsync();

                user = _db.Users.Include(u => u.Wallets).Single(u => u.Wallets.Any(w => w.Address == userToWalletLogin.WalletAddress && w.Provider.ToLower() == userToWalletLogin.WalletProvider.ToLower()));
            }
            
            if (user.Email is not null && !user.EmailConfirmed && _userManager.Options.SignIn.RequireConfirmedEmail) // acc registered earlier (not during current login attempt) that wasn't confirmed yet
            {
                _mapper.Map(user, userToWalletLogin); // to account for isconfirmed
                return new ApiResponse<LoginUserVM>(StatusCodeType.Status401Unauthorized, "Please confirm your email first");
            }

            await _signInManager.SignInAsync(user, userToWalletLogin.RememberMe);
            await _userManager.ResetAccessFailedCountAsync(user);

            _mapper.Map(user, userToWalletLogin);
            userToWalletLogin.Ticket = await GenerateLoginTicketAsync(user.Id, user.PasswordHash, userToWalletLogin.RememberMe);
            
            return new ApiResponse<LoginUserVM>(StatusCodeType.Status200OK, $"You have been successfully logged in with \"{userToWalletLogin.WalletProvider}\" as: \"{userToWalletLogin.UserName}\"", null, userToWalletLogin);
        }

        public async Task<ApiResponse<IList<AuthenticationScheme>>> GetExternalAuthenticationSchemesAsync()
        {
            var externalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            return new ApiResponse<IList<AuthenticationScheme>>(StatusCodeType.Status200OK, "External Authentication Schemes Returned", null, externalLogins);
        }

        public async Task<ApiResponse<AuthenticateUserVM>> LogoutAsync(AuthenticateUserVM userToLogout)
        {
            if (userToLogout == null || !userToLogout.HasAuthenticationStatus(AuthStatus.Authenticated))
                return new ApiResponse<AuthenticateUserVM>(StatusCodeType.Status401Unauthorized, "You are not Authorized so you can't log out");

            await _signInManager.SignOutAsync();
            return new ApiResponse<AuthenticateUserVM>(StatusCodeType.Status200OK, $"You (\"{userToLogout.UserName}\") have been successfully logged out", null, userToLogout);
        }

        public async Task<ApiResponse<ForgotPasswordUserVM>> ForgotPasswordAsync(ForgotPasswordUserVM userWithForgottenPassword)
        {
            var user = await _userManager.FindByEmailAsync(userWithForgottenPassword.Email);
            if (user == null)
                return new ApiResponse<ForgotPasswordUserVM>(StatusCodeType.Status401Unauthorized, "Email not found, please Register first", new[] { new KeyValuePair<string, string>("Email", "There is no User with this Email") }.ToLookup());
            if (!await _userManager.IsEmailConfirmedAsync(user))
                return new ApiResponse<ForgotPasswordUserVM>(StatusCodeType.Status401Unauthorized, "Please Confirm your account first", new[] { new KeyValuePair<string, string>("Email", "Your account hasn't been confirmed yet") }.ToLookup());

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var sendResetEmailResponse = await _emailSender.SendPasswordResetEmailAsync(userWithForgottenPassword.Email, code, userWithForgottenPassword.ReturnUrl);
            if (sendResetEmailResponse.IsError)
                return new ApiResponse<ForgotPasswordUserVM>(StatusCodeType.Status500InternalServerError, "Can't send Password Reset email. Try again later.", null, null, sendResetEmailResponse.ResponseException);

            return new ApiResponse<ForgotPasswordUserVM>(StatusCodeType.Status201Created, $"Change Password link has been sent to: \"{userWithForgottenPassword.Email}\"", null, userWithForgottenPassword);
        }

        public async Task<ApiResponse<ResetPasswordUserVM>> ResetPasswordAsync(ResetPasswordUserVM userToResetPassword)
        {
            var user = await _userManager.FindByEmailAsync(userToResetPassword.Email);
            if (user == null)
                return new ApiResponse<ResetPasswordUserVM>(StatusCodeType.Status401Unauthorized, "There is no User with this Email to Confirm", new[] { new KeyValuePair<string, string>("Email", "No such email") }.ToLookup());

            var resetPasswordResult = await _userManager.ResetPasswordAsync(user, userToResetPassword.ResetPasswordCode.Base58ToUTF8(), userToResetPassword.Password);
            if (!resetPasswordResult.Succeeded)
            {
                var isInvalidToken = resetPasswordResult.Errors.FirstOrDefault(e => e.Code.EqualsInvariant("InvalidToken"));
                if (isInvalidToken != null)
                    return new ApiResponse<ResetPasswordUserVM>(StatusCodeType.Status401Unauthorized, isInvalidToken.Description, new[] { new KeyValuePair<string, string>(nameof(ResetPasswordUserVM.ResetPasswordCode), isInvalidToken.Description) }.ToLookup());
                var errors = resetPasswordResult.Errors.ToLookup(userToResetPassword.GetPropertyNames());
                return new ApiResponse<ResetPasswordUserVM>(StatusCodeType.Status401Unauthorized, "Password Reset Failed", errors);
            }

            if (await _userManager.IsLockedOutAsync(user))
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow);

            return new ApiResponse<ResetPasswordUserVM>(StatusCodeType.Status200OK, $"Password for User: \"{userToResetPassword.UserName}\" has been changed successfully", null, _mapper.Map(user, userToResetPassword));
        }

        public async Task<ApiResponse<bool>> CheckUserResetPasswordCodeAsync(CheckResetPasswordCodeUserVM userToCheckResetPasswordCode)
        {
            if (userToCheckResetPasswordCode.UserName.IsNullOrWhiteSpace() || userToCheckResetPasswordCode.CheckResetPasswordCode.IsNullOrWhiteSpace())
                return new ApiResponse<bool>(StatusCodeType.Status200OK, "Reset Password Code is incorrect", null, false);

            var user = _db.Users.Single(u => u.UserName.ToLower() == userToCheckResetPasswordCode.UserName.ToLower());
            var verificationResult = await _passwordResetTokenProvider.ValidateAsync(null, userToCheckResetPasswordCode.CheckResetPasswordCode.Base58ToUTF8(), _userManager, user);
            if (!verificationResult)
                return new ApiResponse<bool>(StatusCodeType.Status200OK, "Reset Password Code is incorrect", null, false);
            return await Task.FromResult(new ApiResponse<bool>(StatusCodeType.Status200OK, "Reset Password Code is correct", null, true));
        }

        public async Task<ApiResponse<bool>> CheckUserPasswordAsync(CheckPasswordUserVM userToCheckPassword)
        {
            if ((userToCheckPassword.Id == Guid.Empty && userToCheckPassword.UserName.IsNullOrWhiteSpace()))
                return new ApiResponse<bool>(StatusCodeType.Status200OK, "Password is incorrect", null, false);

            var user = userToCheckPassword.Id != Guid.Empty ? _db.Users.Single(u => u.Id == userToCheckPassword.Id) : _db.Users.Single(u => u.UserName.ToLower() == userToCheckPassword.UserName.ToLower());
            if (user.PasswordHash is null)
                return userToCheckPassword.Password is null
                    ? new ApiResponse<bool>(StatusCodeType.Status200OK, "Password is correct", null, true)
                    : new ApiResponse<bool>(StatusCodeType.Status200OK, "Password is incorrect", null, false);
            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, userToCheckPassword.Password);
            if (verificationResult != PasswordVerificationResult.Success)
                return new ApiResponse<bool>(StatusCodeType.Status200OK, "Password is incorrect", null, false);
            return await Task.FromResult(new ApiResponse<bool>(StatusCodeType.Status200OK, "Password is correct", null, true));
        }

        public async Task<ApiResponse<EditUserVM>> EditAsync(AuthenticateUserVM authUser, EditUserVM userToEdit)
        {
            authUser = (await GetAuthenticatedUserAsync(null, null, authUser))?.Result;
            if (authUser == null || authUser.AuthenticationStatus != AuthStatus.Authenticated)
                return new ApiResponse<EditUserVM>(StatusCodeType.Status401Unauthorized, "You are not Authorized to Edit User Data");
            if (!(await new EditUserVMValidator(this).ValidateAsync(userToEdit)).IsValid)
                return new ApiResponse<EditUserVM>(StatusCodeType.Status404NotFound, "Supplied data is invalid");

            userToEdit.Id = authUser.Id; // to fix the case when malicious user edited the Id manually
            var user = await _db.Users.Include(u => u.Avatar).SingleOrDefaultAsync(u => u.Id == userToEdit.Id);
            if (user is null)
                return new ApiResponse<EditUserVM>(StatusCodeType.Status404NotFound, "There is no User with this Id", new[] { new KeyValuePair<string, string>("Id", "There is no DbUser with the supplied Id") }.ToLookup());

            var tempAvatarDir = PathUtils.Combine(PathSeparator.BSlash, FileUtils.GetEntryAssemblyDir(), "UserFiles", authUser.UserName, "_temp/Avatars");
            var newAvatar = Directory.GetFiles(tempAvatarDir).NullifyIf(fs => !fs.Any())?.MaxBy_(f => new FileInfo(f).CreationTimeUtc)?.Last()?.PathToFileData(true);

            var userNameChanged = !userToEdit.UserName.EqualsIgnoreCase(user.UserName);
            var emailChanged = !userToEdit.Email.EqualsIgnoreCase(user.Email);
            var passwordChanged = !userToEdit.NewPassword.IsNullOrWhiteSpace() && !userToEdit.OldPassword.EqualsInvariant(userToEdit.NewPassword);
            var avatarChanged = newAvatar is not null && user.Avatar?.Hash?.EqualsInvariant(newAvatar.Hash) != true;
            var avatarShouldBeRemoved = userToEdit.Avatar == FileData.Empty;
            var isConfirmationRequired = emailChanged && _userManager.Options.SignIn.RequireConfirmedEmail;

            if (!userNameChanged && !emailChanged && !passwordChanged && !avatarChanged && !avatarShouldBeRemoved)
                return new ApiResponse<EditUserVM>(StatusCodeType.Status404NotFound, "User data has not changed so there is nothing to update");

            var propsToChange = new List<string>();

            if (userNameChanged)
            {
                user.UserName = userToEdit.UserName;
                propsToChange.Add(nameof(user.UserName));
            }

            if (emailChanged)
            {
                user.Email = userToEdit.Email;
                user.NormalizedEmail = userToEdit.Email.ToUpperInvariant();
                if (isConfirmationRequired)
                    user.EmailConfirmed = false;
                propsToChange.Add(nameof(user.Email));
            }

            if (passwordChanged)
            {
                var isOldPasswordCorrect = user.PasswordHash is null || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, userToEdit.OldPassword) == PasswordVerificationResult.Success;
                if (!isOldPasswordCorrect) // user should be allowed to change password if he didn't set one at all (was logging in exclusively with an external provider) or if he provided correct Old Password to his Account
                    return new ApiResponse<EditUserVM>(StatusCodeType.Status401Unauthorized, "Old Password is not Correct", new[] { new KeyValuePair<string, string>("OldPassword", "Incorrect Password") }.ToLookup());

                var errors = new List<IdentityError>();
                foreach (var v in _userManager.PasswordValidators)
                    errors.AddRange((await v.ValidateAsync(_userManager, user, userToEdit.NewPassword)).Errors);
                if (errors.Any())
                    return new ApiResponse<EditUserVM>(StatusCodeType.Status401Unauthorized, "New Password is Invalid", errors.ToLookup(userToEdit.GetPropertyNames().Append("Password")).RenameKey("Password", nameof(userToEdit.NewPassword)));

                user.PasswordHash = _passwordHasher.HashPassword(user, userToEdit.NewPassword); // use db directly to override identity validation because we want to be able to provide password for a null hash if user didn't set password before
                userToEdit.Ticket = await GenerateLoginTicketAsync(userToEdit.Id, user.PasswordHash, authUser.RememberMe);
                userToEdit.HasPassword = true;

                propsToChange.Add("Password");
            }

            if (avatarChanged || avatarShouldBeRemoved)
            {
                //user.Avatar = newAvatar.ToDbFile(user.Id, user.Id);
                var dbAvatar = _db.Files.SingleOrDefault(f => f.UserHavingFileAsAvatarId == user.Id);
                if (dbAvatar is not null)
                {
                    dbAvatar.UserHavingFileAsAvatarId = null;
                    await _db.SaveChangesAsync();
                }

                if (avatarChanged)
                    _db.Files.AddOrUpdate(newAvatar.ToDbFile(user.Id, user.Id), f => f.Hash); // or this and set userid in avatar to null first

                userToEdit.Avatar = newAvatar;
                FileUtils.EmptyDir(tempAvatarDir);
                propsToChange.Add(nameof(user.Avatar));
            }

            await _db.SaveChangesAsync();

            if (isConfirmationRequired)
            {
                userToEdit.ReturnUrl = $"{ConfigUtils.FrontendBaseUrl}/Account/ConfirmEmail?email={user.Email}";
                userToEdit.ShouldLogout = true;

                var resendConfirmationResult = await ResendConfirmationEmailAsync(_mapper.Map(userToEdit, new ResendConfirmationEmailUserVM()));
                if (resendConfirmationResult.IsError)
                    return new ApiResponse<EditUserVM>(StatusCodeType.Status400BadRequest, "User Details have been Updated buy system can't resend Confirmation Email. Please try again later.");

                await _signInManager.SignOutAsync();
                return new ApiResponse<EditUserVM>(StatusCodeType.Status202Accepted, $"Successfully updated User \"{userToEdit.UserName}\" with new {propsToChange.Select(p => $"\"{p}\"").JoinAsString(", ").ReplaceLast(",", " and")}, since you have updated your email address the confirmation code has been sent to: \"{userToEdit.Email}\"", userToEdit);
            }

            await _signInManager.SignInAsync(user, authUser.RememberMe);
            return new ApiResponse<EditUserVM>(StatusCodeType.Status202Accepted, $"Successfully updated User \"{userToEdit.UserName}\" with new {propsToChange.Select(p => $"\"{p}\"").JoinAsString(", ").ReplaceLast(",", " and")}", userToEdit);
        }

    }
}
