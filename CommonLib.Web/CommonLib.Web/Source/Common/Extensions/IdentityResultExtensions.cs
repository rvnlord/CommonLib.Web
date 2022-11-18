using CommonLib.Web.Source.Common.Extensions.Collections;
using Microsoft.AspNetCore.Identity;

namespace CommonLib.Web.Source.Common.Extensions
{
    public static class IdentityResultExtensions
    {
        public static string FirstError(this IdentityResult identityResult) => identityResult?.Errors.FirstMessage() ?? string.Empty;
    }
}