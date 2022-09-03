using System;
using System.Linq;
using System.Threading.Tasks;
using CommonLib.Web.Source.Models;
using CommonLib.Web.Source.Models.Interfaces;
using CommonLib.Web.Source.Services.Account.Interfaces;
using CommonLib.Web.Source.ViewModels.Account;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Source.Models;
using CommonLib.Web.Source.Models.Account;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace CommonLib.Web.Source.Controllers
{
    [Route("api/account"), ApiController]
    public class AccountController : MyControllerBase
    {
        private readonly IAccountManager _accountManager;

        public AccountController(IAccountManager accountManager)
        {
            _accountManager = accountManager;
        }

        [HttpPost("login")] // POST: api/account/login
        public async Task<JToken> LoginAsync(LoginUserVM user) => await EnsureResponseAsync(async () => await _accountManager.LoginAsync(user));

        [HttpGet("externallogin")] // GET: api/account/externallogin
        public async Task<IActionResult> ExternalLoginAsync(string provider, string returnUrl, string rememberMe)
        {
            try
            {
                var (authenticationProperties, schemaName) = await _accountManager.ExternalLoginAsync(new LoginUserVM { ExternalProvider = provider, ReturnUrl = returnUrl.HtmlDecode(), RememberMe = Convert.ToBoolean(rememberMe) });
                return Challenge(authenticationProperties, schemaName);
            }
            catch (ArgumentNullException ex)
            {
                return Redirect(ex.Message);
            }
        }

        [HttpGet("externallogincallback")] // GET: api/account/externallogincallback
        public async Task<IActionResult> ExternalLoginCallbackAsync(string returnUrl = null, string remoteError = null) => Redirect(await _accountManager.ExternalLoginCallbackAsync(returnUrl, remoteError));

        [HttpPost("externalloginauthorize")] // POST: api/account/externalloginauthorize
        public async Task<JToken> ExternalLoginAuthorizeAsync(JToken jLoginUserVM) => await EnsureResponseAsync(async () => await _accountManager.ExternalLoginAuthorizeAsync(jLoginUserVM.To<LoginUserVM>()));

        [HttpPost("register")] // POST: api/account/register
        public async Task<JToken> RegisterAsync(JToken jRegisterUser) => await EnsureResponseAsync(async () => await _accountManager.RegisterAsync(jRegisterUser.To<RegisterUserVM>()));

        [HttpPost("confirmemail")] // POST: api/account/confirmemail
        public async Task<JToken> ConfirmEmailAsync(JToken jConfirmUserEmail) => await EnsureResponseAsync(async () => await _accountManager.ConfirmEmailAsync(jConfirmUserEmail.To<ConfirmUserVM>()));
        
        [HttpPost("finduserbyemail")] // POST: api/account/finduserbyemail
        public async Task<JToken> FindUserByEmailAsync(JToken jEmail) => await EnsureResponseAsync(async () => await _accountManager.FindUserByEmailAsync(jEmail is JValue ? jEmail.ToString() : jEmail["email"]?.ToString()));

        [HttpPost("finduserbyname")] // POST: api/account/finduserbyname
        public async Task<JToken> FindUserByNameAsync(JToken jName) => await EnsureResponseAsync(async () => await _accountManager.FindUserByNameAsync(jName is JValue ? jName.ToString() : jName["name"]?.ToString()));
        
        [HttpPost("finduserbyconfirmationcode")] // POST: api/account/finduserbyconfirmationcode
        public async Task<JToken> FindUserByConfirmationCodeAsync(JToken jConfirmationCode) => await EnsureResponseAsync(async () => await _accountManager.FindUserByConfirmationCodeAsync(jConfirmationCode is JValue ? jConfirmationCode.ToString() : jConfirmationCode["confirmationcode"]?.ToString()));

        [HttpPost("checkusermanagercompliance")] // POST: api/account/checkusermanagercompliance
        public async Task<JToken> CheckUserManagerCompliance(JToken jUserPropertyNameUserPropertyDisplayNameAndUserPropertValue) => await EnsureResponseAsync(async () => await _accountManager.CheckUserManagerComplianceAsync(jUserPropertyNameUserPropertyDisplayNameAndUserPropertValue["UserPropertyName"]?.ToString(), jUserPropertyNameUserPropertyDisplayNameAndUserPropertValue["UserPropertyDisplayName"]?.ToString(), jUserPropertyNameUserPropertyDisplayNameAndUserPropertValue["UserPropertyValue"]?.ToString()));
        
        [HttpPost("authenticateuser")] // POST: api/account/authenticateuser
        public async Task<JToken> GetAuthenticatedUserAsync(JToken jAuthenticateUser) => await EnsureResponseAsync(async () => await _accountManager.GetAuthenticatedUserAsync(HttpContext, User, jAuthenticateUser.To<AuthenticateUserVM>()));

        [HttpPost("getexternalauthenticationschemes")] // POST: api/account/getexternalauthenticationschemes
        public async Task<JToken> GetExternalAuthenticationSchemesAsync() => await EnsureResponseAsync(async () => await _accountManager.GetExternalAuthenticationSchemesAsync());

        [HttpPost("forgotpassword")] // POST: api/account/forgotpassword
        public async Task<JToken> ForgotPasswordAsync(JToken jForgotPasswordUserVM) => (ModelState.IsValid ? await _accountManager.ForgotPasswordAsync(jForgotPasswordUserVM.To<ForgotPasswordUserVM>()) : _defaultInvalidResponse).ToJToken();

        //[HttpPost("resetpassword")] // POST: api/account/resetpassword
        //public async Task<JToken> ResetPasswordAsync(JToken JResetPasswordUserVM) => (ModelState.IsValid ? await _accountManager.ResetPasswordAsync(JResetPasswordUserVM.To<ResetPasswordUserVM>()) : _defaultInvalidResponse).ToJToken();

        [HttpPost("resendconfirmationemail")] // POST: api/account/resendconfirmationemail
        public async Task<JToken> ResendConfirmationEmailAsync(JToken JResendEmailConfirmationUserVM) => await EnsureResponseAsync(async () => await _accountManager.ResendConfirmationEmailAsync(JResendEmailConfirmationUserVM.To<ResendConfirmationEmailUserVM>()));

        //[HttpPost("setfrontendbaseurl")] // POST: api/account/setfrontendbaseurl
        //public async Task<JToken> SetFrontendBaseUrlAsync(JToken jFrontendBaseUrl) => (ModelState.IsValid ? await _accountManager.SetFrontEndBaseUrl(jFrontendBaseUrl.To<string>()) : _defaultInvalidResponse).ToJToken();

        //[HttpPost("verifyuserpassword")] // POST: api/account/verifyuserpassword
        //public JToken VerifyUserPassword(JToken jUserIdAndPassword) => (ModelState.IsValid ? _accountManager.VerifyUserPassword(jUserIdAndPassword["userId"].To<Guid>(), jUserIdAndPassword["password"]?.ToString()) : _defaultInvalidResponse).ToJToken();

        //[HttpPost("edit")] // POST: api/account/edit
        //public async Task<JToken> EditAsync(JToken JAuthUserAndEditUser) => (ModelState.IsValid ? await _accountManager.EditAsync(JAuthUserAndEditUser["AuthenticatedUser"]?.To<AuthenticateUserVM>(), JAuthUserAndEditUser["UserToEdit"].To<EditUserVM>()) : _defaultInvalidResponse).ToJToken();

        [HttpPost("logout")] // POST: api/account/logout
        public async Task<JToken> LogoutAsync(JToken JAuthUser) => await EnsureResponseAsync(async () => await _accountManager.LogoutAsync(JAuthUser.To<AuthenticateUserVM>()));
    }
}
