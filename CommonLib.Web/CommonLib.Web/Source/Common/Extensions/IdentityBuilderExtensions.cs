using CommonLib.Source.Common.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CommonLib.Web.Source.Common.Extensions
{
    public static class IdentityBuilderExtensions
    {//.AddCustomTokenProvider<CustomEmailConfirmationTokenProvider<DbUser>>("CustomEmailConfirmationTokenProvider");
        public static IdentityBuilder AddCustomTokenProvider<T>(this IdentityBuilder ib) where T : class
        {
            ib.Services.Configure<IdentityOptions>(options =>
            {
                options.Tokens.ProviderMap[typeof(T).Name.BeforeFirst("`")] = new TokenProviderDescriptor(typeof(T));
            });
            ib.Services.AddTransient<T>();
            return ib; 
        }
    }
}
