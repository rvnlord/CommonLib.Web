using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using BlazorDemo.Common.Models.Account;
using CommonLib.Web.Source.DbContext;
using CommonLib.Web.Source.Models;
using CommonLib.Web.Source.Models.Account;
using CommonLib.Web.Source.Services.Account.Interfaces;
using CommonLib.Web.Source.ViewModels.Account;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommonLib.Web.Source.Services.Account
{
    public class AccountManager : IAccountManager
    {
        private readonly UserManager<User> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly SignInManager<User> _signInManager;
        private readonly AccountDbContext _db;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _http;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;

        public AccountManager(UserManager<User> userManager,
            SignInManager<User> signInManager,
            IEmailSender emailSender,
            AccountDbContext db,
            IMapper autoMapper,
            IHttpContextAccessor http,
            IPasswordHasher<User> passwordHasher,
            RoleManager<IdentityRole<Guid>> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _db = db;
            _mapper = autoMapper;
            _http = http;
            _passwordHasher = passwordHasher;
            _roleManager = roleManager;
        }

        public async Task<ApiResponse<FindUserVM>> FindUserByNameAsync(string name)
        {
            var user = await IEnumerableExtensions.SingleOrDefaultAsync(_db.Users, u => u.UserName.ToLower() == name.ToLower());
            if (user == null)
                return new ApiResponse<FindUserVM>(StatusCodeType.Status404NotFound, "There is no User with the given Name", null);

            var foundUser = _mapper.Map(user, new FindUserVM());
            foundUser.Roles = (await _userManager.GetRolesAsync(user)).Select(r => new FindRoleVM { Name = r }).ToList();
            foundUser.Claims = (await _userManager.GetClaimsAsync(user)).Select(c => new FindClaimVM { Name = c.Type }).Where(c => !c.Name.EqualsIgnoreCase("Email")).ToList();

            return new ApiResponse<FindUserVM>(StatusCodeType.Status200OK, "Finding User by Name has been Successful", null, foundUser);
        }

        public async Task<ApiResponse<FindUserVM>> FindUserByEmailAsync(string email)
        {
            var user = await IEnumerableExtensions.SingleOrDefaultAsync(_db.Users, u => u.Email.ToLower() == email.ToLower());
            if (user == null)
                return new ApiResponse<FindUserVM>(StatusCodeType.Status404NotFound, "There is no User with the given Email", null);

            var foundUser = _mapper.Map(user, new FindUserVM());
            foundUser.Roles = (await _userManager.GetRolesAsync(user)).Select(r => new FindRoleVM { Name = r }).ToList();
            foundUser.Claims = (await _userManager.GetClaimsAsync(user)).Select(c => new FindClaimVM { Name = c.Type }).Where(c => !c.Name.EqualsIgnoreCase("Email")).ToList();

            return new ApiResponse<FindUserVM>(StatusCodeType.Status200OK, "Finding User by Email has been Successful", null, foundUser);
        }

        public async Task<ApiResponse<bool>> CheckUserManagerComplianceAsync(string userPropertyName, string userPropertyDisplayName, string userPropertyValue)
        {
            await Task.CompletedTask;

            if (userPropertyValue == null)
                return new ApiResponse<bool>(StatusCodeType.Status200OK, $"{userPropertyDisplayName} is Empty, falling through to other attributes", null, true);

            if (userPropertyName.ContainsIgnoreCase("UserName"))
            {
                if (!userPropertyValue.All(c => c.In(_userManager.Options.User.AllowedUserNameCharacters))) // we are skipping unique email check here because we are already checking email in other attrbiute and because email is not part of username property despite the fact that usermanager have email option under User category for some reason
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
            userToAuthenticate.IsAuthenticated = false;
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

            User user = null;
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
            userToAuthenticate.IsAuthenticated = true;
            userToAuthenticate.Roles = (await _userManager.GetRolesAsync(user)).Select(r => new FindRoleVM { Name = r }).ToList();
            userToAuthenticate.Claims = (await _userManager.GetClaimsAsync(user)).Select(c => new FindClaimVM { Name = c.Type }).Where(c => !c.Name.EqualsIgnoreCase("Email")).ToList();
            return new ApiResponse<AuthenticateUserVM>(StatusCodeType.Status200OK, "Getting Authenticated User was Successful", null, userToAuthenticate);
        }

        public async Task<ApiResponse<RegisterUserVM>> RegisterAsync(RegisterUserVM userToRegister)
        {
            var user = new User { UserName = userToRegister.UserName, Email = userToRegister.Email };
            var result = userToRegister.Password != null ? await _userManager.CreateAsync(user, userToRegister.Password) : await _userManager.CreateAsync(user);
            if (!result.Succeeded) // new List<IdentityError> { new() { Code = "Password", Description = "Password Error TEST" } }
            {
                var errors = result.Errors.ToLookup(userToRegister.GetPropertyNames());
                return new ApiResponse<RegisterUserVM>(StatusCodeType.Status401Unauthorized, "Invalid Model", errors, null);
            }

            await _userManager.AddClaimAsync(user, new Claim("Email", user.Email));

            if (!await _roleManager.RoleExistsAsync("User"))
                await _roleManager.CreateAsync(new IdentityRole<Guid>("User"));

            await _userManager.AddToRoleAsync(user, "User");

            if (_userManager.Options.SignIn.RequireConfirmedEmail)
            {
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var deployingEmailResponse = await _emailSender.SendConfirmationEmailAsync(userToRegister.Email, code, userToRegister.ReturnUrl.UTF8ToBase58());
                if (deployingEmailResponse.IsError)
                {
                    userToRegister.ReturnUrl = $"/Account/ResendEmailConfirmation/?email={userToRegister.Email}&returnUrl={userToRegister.ReturnUrl.UTF8ToBase58()}";
                    return new ApiResponse<RegisterUserVM>(StatusCodeType.Status500InternalServerError, "Registration had been Successful, but the email wasn't sent. Try again later.", null, userToRegister, deployingEmailResponse.ResponseException);
                }

                userToRegister.ReturnUrl = $"/Account/ConfirmEmail?email={userToRegister.Email}&returnUrl={userToRegister.ReturnUrl.UTF8ToBase58()}";
                return new ApiResponse<RegisterUserVM>(StatusCodeType.Status201Created, $"Registration for User \"{userToRegister.UserName}\" has been successful, activation email has been deployed to: \"{userToRegister.Email}\"", null, _mapper.Map(userToRegister, new RegisterUserVM()));
            }

            userToRegister.ReturnUrl = $"/Account/Login?returnUrl={userToRegister.ReturnUrl.UTF8ToBase58()}";
            userToRegister.Ticket = await GenerateLoginTicketAsync(user.Id, user.PasswordHash, false);
            await _signInManager.SignInAsync(user, false);
            return new ApiResponse<RegisterUserVM>(StatusCodeType.Status201Created, $"Registration for User \"{userToRegister.UserName}\" has been successful, you are now logged in", null, userToRegister);
        }

        public async Task<ApiResponse<ConfirmUserVM>> ConfirmEmailAsync(ConfirmUserVM userToConfirm)
        {
            var user = await _userManager.FindByEmailAsync(userToConfirm.Email);
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
            return new ApiResponse<ConfirmUserVM>(StatusCodeType.Status200OK, "Email has been Confirmed Successfully", null, _mapper.Map(user, new ConfirmUserVM()));
        }

        public async Task<string> GenerateLoginTicketAsync(Guid id, string passwordHash, bool rememberMe)
        {
            var key = (await _db.CryptographyKeys.AsNoTracking().SingleOrDefaultAsync(k => k.Name.ToLower() == "LoginTicket"))?.Value;
            if (key == null)
            {
                key = CryptoUtils.GenerateCamelliaKey().ToBase58String();
                await _db.CryptographyKeys.AddAsync(new CryptographyKey { Name = "LoginTicket", Value = key });
                await _db.SaveChangesAsync();
            }

            return $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}|{id}|{passwordHash}|{Convert.ToInt32(rememberMe)}".UTF8ToByteArray()
                .EncryptCamellia(key.Base58ToByteArray()).ToBase58String();
        }

        public async Task<ApiResponse<FindUserVM>> FindUserByConfirmationCodeAsync(string confirmationCode)
        {
            var user = await IEnumerableExtensions.SingleOrDefaultAsync(_db.Users, u => u.EmailActivationToken?.ToLower() == confirmationCode.ToLower());
            if (user == null)
                return new ApiResponse<FindUserVM>(StatusCodeType.Status404NotFound, "There is no User with the given Activation Code", null);

            var foundUser = _mapper.Map(user, new FindUserVM());
            foundUser.Roles = (await _userManager.GetRolesAsync(user)).Select(r => new FindRoleVM { Name = r }).ToList();
            foundUser.Claims = (await _userManager.GetClaimsAsync(user)).Select(c => new FindClaimVM { Name = c.Type }).Where(c => !c.Name.EqualsIgnoreCase("Email")).ToList();

            return new ApiResponse<FindUserVM>(StatusCodeType.Status200OK, "Finding User by Activation Code has been Successful", null, foundUser);
        }

        public async Task<ApiResponse<ResendConfirmationEmailUserVM>> ResendConfirmationEmailAsync(ResendConfirmationEmailUserVM userToResendConfirmationEmail)
        {
            var successMessage = $"Confirmation email has been sent to: \"{userToResendConfirmationEmail.Email}\" if there is an account associated with it";
            userToResendConfirmationEmail.ReturnUrl += $"?email={userToResendConfirmationEmail.Email}";
            var emailReturnUrl = PathUtils.Combine(PathSeparator.FSlash, ConfigUtils.FrontendBaseUrl, "Account/Login");
            var user = await _userManager.FindByEmailAsync(userToResendConfirmationEmail.Email);
            if (user == null)
                return new ApiResponse<ResendConfirmationEmailUserVM>(StatusCodeType.Status200OK, successMessage, null, userToResendConfirmationEmail);

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                userToResendConfirmationEmail.ReturnUrl = emailReturnUrl;
                return new ApiResponse<ResendConfirmationEmailUserVM>(StatusCodeType.Status200OK, $"Email \"{userToResendConfirmationEmail.Email}\" has already been confirmed", null, userToResendConfirmationEmail);
            }

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var resendingConfirmationEmailResponse = await _emailSender.SendConfirmationEmailAsync(userToResendConfirmationEmail.Email, code, emailReturnUrl.UTF8ToBase58());
            if (resendingConfirmationEmailResponse.IsError)
                return new ApiResponse<ResendConfirmationEmailUserVM>(StatusCodeType.Status500InternalServerError, "Can't resend Confirmation Email. Please try again later.", null, null, resendingConfirmationEmailResponse.ResponseException);

            return new ApiResponse<ResendConfirmationEmailUserVM>(StatusCodeType.Status200OK, successMessage, null, _mapper.Map(user, userToResendConfirmationEmail));
        }

        public async Task<ApiResponse<LoginUserVM>> LoginAsync(LoginUserVM userToLogin)
        {
            if (userToLogin.UserName.IsNullOrWhiteSpace())
                return new ApiResponse<LoginUserVM>(StatusCodeType.Status401Unauthorized, "User Name can't be empty", new[] { new KeyValuePair<string, string>("UserName", "User Name is required") }.ToLookup());

            var user = await _userManager.FindByNameAsync(userToLogin.UserName);
            if (user == null)
                return new ApiResponse<LoginUserVM>(StatusCodeType.Status401Unauthorized, "User Name not found, please Register first", new[] { new KeyValuePair<string, string>("UserName", "There is no User with this UserName") }.ToLookup());

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

            var reqScheme = _http.HttpContext.Request.Scheme; // we need API url here which is different than the Web App one
            var host = _http.HttpContext.Request.Host;
            var pathbase = _http.HttpContext.Request.PathBase;
            var redirectUrl = $"{reqScheme}://{host}{pathbase}/api/account/externallogincallback?returnUrl={userToExternalLogin.ReturnUrl}&user={userToExternalLogin.JsonSerialize().UTF8ToBase58()}";
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(userToExternalLogin.ExternalProvider, redirectUrl);

            return (properties, scheme.Name);
        }

        public async Task<string> ExternalLoginCallbackAsync(string returnUrl, string remoteError)
        {
            var userToExternalLogin = _http.HttpContext.Request.Query["user"].ToString().Base58ToUTF8().JsonDeserialize().To<LoginUserVM>();
            var decodedReturnUrl = returnUrl.Base58ToUTF8();
            var url = decodedReturnUrl.BeforeFirstOrWhole("?");
            var qs = decodedReturnUrl.QueryStringToDictionary();
            qs["remoteStatus"] = "Error";

            try
            {
                if (remoteError != null)
                {
                    qs["remoteMessage"] = remoteError.UTF8ToBase58();
                    return $"{url}?{qs.ToQueryString()}";
                }

                var externalLoginInfo = await _signInManager.GetExternalLoginInfoAsync();
                if (externalLoginInfo == null)
                {
                    qs["remoteMessage"] = "Error loading external login information".UTF8ToBase58();
                    return $"{url}?{qs.ToQueryString()}";
                }

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
            var user = await _userManager.FindByEmailAsync(userToExternalLogin.Email);
            if (user == null)
            {
                var userToRegister = new RegisterUserVM { UserName = userToExternalLogin.UserName, Email = userToExternalLogin.Email };
                var registerUserResponse = await RegisterAsync(userToRegister);
                if (registerUserResponse.IsError)
                    return new ApiResponse<LoginUserVM>(StatusCodeType.Status401Unauthorized, registerUserResponse.Message, null);
                user = await _userManager.FindByEmailAsync(userToExternalLogin.Email);
            }

            if (!user.EmailConfirmed && _userManager.Options.SignIn.RequireConfirmedEmail)
                return new ApiResponse<LoginUserVM>(StatusCodeType.Status401Unauthorized, "Please Confirm your email first", null);

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

                    return new ApiResponse<LoginUserVM>(StatusCodeType.Status401Unauthorized, message, null);
                }

                var secondAttemptExternalLoginResult = await _signInManager.ExternalLoginSignInAsync(userToExternalLogin.ExternalProvider, userToExternalLogin.ExternalProviderKey, userToExternalLogin.RememberMe, true);
                if (!secondAttemptExternalLoginResult.Succeeded)
                    return new ApiResponse<LoginUserVM>(StatusCodeType.Status401Unauthorized, "User didn't have an External Login Account so it was added, but Login Attempt has Failed", null);
            }

            await _signInManager.SignInAsync(user, userToExternalLogin.RememberMe);
            await _userManager.ResetAccessFailedCountAsync(user);
            userToExternalLogin.Ticket = await GenerateLoginTicketAsync(user.Id, user.PasswordHash, userToExternalLogin.RememberMe);
            return new ApiResponse<LoginUserVM>(StatusCodeType.Status200OK, $"You have been successfully logged in with an External Provider as: \"{_mapper.Map(user, userToExternalLogin).UserName}\"", null, userToExternalLogin);
        }

        public async Task<ApiResponse<IList<AuthenticationScheme>>> GetExternalAuthenticationSchemesAsync()
        {
            var externalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            return new ApiResponse<IList<AuthenticationScheme>>(StatusCodeType.Status200OK, "External Authentication Schemes Returned", null, externalLogins);
        }
    }
}
