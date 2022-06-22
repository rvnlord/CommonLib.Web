using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using CommonLib.Web.Source.ViewModels.Account;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using Microsoft.Collections.Extensions;

namespace CommonLib.Web.Source.Common.Converters
{
    public static class ClaimConverter
    {
        public static MultiValueDictionary<string, string> ToStringDictionary(this IEnumerable<Claim> claims, params string[] exclude)
        {
            return claims.Select(c => new KeyValuePair<string, string>(typeof(ClaimTypes).GetConstants<string>()
                    .Select(ct => new { ct.Key, ct.Value }).SingleOrDefault(ct => ct.Value.EqualsInvariant(c.Type))?.Key ?? c.Type, c.Value)).Distinct()
                .Where(kvp => !kvp.Key.EqAnyIgnoreCase(exclude)).ToMultiValueDictionary();
        }

        public static List<(string, string)> ToNameValueList(this List<FindClaimVM> claims)
        {
            return (
                from claim in claims 
                from claimValue in claim.Values 
                select (claim.Name, claimValue.Value)).ToList();
        }
    }
}