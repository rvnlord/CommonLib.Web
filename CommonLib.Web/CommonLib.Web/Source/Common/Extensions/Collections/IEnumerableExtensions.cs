using System.Collections.Generic;
using System.Linq;
using CommonLib.Source.Common.Extensions;
using Microsoft.AspNetCore.Identity;

namespace CommonLib.Web.Source.Common.Extensions.Collections
{
    public static class IEnumerableExtensions
    {
        public static string FirstMessage(this IEnumerable<IdentityError> identityErrors)
        {
            var errors = identityErrors?.ToArray();
            if (identityErrors == null || !errors.Any() || errors.First().Code.IsNullOrWhiteSpace() || errors.First().Description.IsNullOrWhiteSpace())
                return string.Empty;

            return $"[{errors.First().Code}] {errors.First().Description}";
        }
    }
}
