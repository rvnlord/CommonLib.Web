using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

public static class SignInManagerExtensions
{
    public static async Task<SignInResult> SignInOrTwoFactorAsync<TUser>(this SignInManager<TUser> signInManager, TUser user, bool isPersistent, string loginProvider = null, bool bypassTwoFactor = false) where TUser : class
    {
        var miSignInOrTwoFactorAsync = typeof(SignInManager<TUser>).GetMethod("SignInOrTwoFactorAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        if (miSignInOrTwoFactorAsync is null)
            throw new NullReferenceException("The method 'SignInOrTwoFactorAsync' was not found.");
        return await ((Task<SignInResult>)miSignInOrTwoFactorAsync.Invoke(signInManager, new object[] { user, isPersistent, loginProvider, bypassTwoFactor }) ?? throw new NullReferenceException("The method 'SignInOrTwoFactorAsync' Task was null."));
    }
}