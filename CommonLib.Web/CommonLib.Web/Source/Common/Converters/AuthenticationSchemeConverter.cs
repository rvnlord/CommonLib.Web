using System.Collections.Generic;
using System.Linq;
using CommonLib.Source.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;

namespace CommonLib.Web.Source.Common.Converters
{
    public static class AuthenticationSchemeConverter
    {
        public static AuthenticationSchemeVM ToAuthenticationSchemeVM(this AuthenticationScheme authenticationScheme)
        {
            return new AuthenticationSchemeVM(authenticationScheme.Name, authenticationScheme.DisplayName, authenticationScheme.HandlerType);
        }

        public static List<AuthenticationSchemeVM> ToAuthenticationSchemeVMs(this IEnumerable<AuthenticationScheme> authenticationSchemes)
        {
            return authenticationSchemes.Select(a => a.ToAuthenticationSchemeVM()).ToList();
        }
    }
}
