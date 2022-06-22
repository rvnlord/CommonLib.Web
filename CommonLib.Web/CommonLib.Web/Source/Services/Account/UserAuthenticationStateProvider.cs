using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Converters;
using CommonLib.Web.Source.Services.Account.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;

namespace CommonLib.Web.Source.Services.Account
{
    public class UserAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IAccountClient _accountClient;

        public UserAuthenticationStateProvider(IAccountClient accountClient)
        {
            _accountClient = accountClient;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var authenticationResponse = await _accountClient.GetAuthenticatedUserAsync();
            await Task.Delay(10);
            if (authenticationResponse.IsError)
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

            var authenticatingUser = authenticationResponse.Result;
            if (!authenticatingUser.IsAuthenticated)
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

            var claims = new[] { new Claim(ClaimTypes.Name, authenticatingUser.UserName) }.Concat(authenticatingUser.Claims.ToNameValueList().Select(c => new Claim(c.Item1, c.Item2)));
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, "name", "role");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }

        public void StateChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
