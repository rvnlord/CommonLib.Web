using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommonLib.Web.Source.Models;
using CommonLib.Web.Source.Services.Account.Interfaces;
using CommonLib.Web.Source.ViewModels.Account;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Source.Models;
using FluentValidation;

namespace CommonLib.Web.Source.Common.Extensions
{
    public static class IRuleBuilderOptionsExtensions
    {
        public static IRuleBuilderOptions<T, string> RequiredWithMessage<T>(this IRuleBuilder<T, string> rb)
        {
            return rb.Must((_, value, _) => !value.IsNullOrWhiteSpace()).WithMessage($"{rb.GetPropertyDisplayName()} is required");
        }

        public static IRuleBuilderOptions<T, string> MinLengthWithMessage<T>(this IRuleBuilder<T, string> rb, int minLength)
        {
            return rb.Must((_, value, _) => value.Length >= minLength).WithMessage((_, value) => $"{rb.GetPropertyDisplayName()} \"{value}\" must contain at least {minLength} characters");
        }

        public static IRuleBuilderOptions<T, string> MaxLengthWithMessage<T>(this IRuleBuilder<T, string> rb, int maxLength)
        {
            return rb.Must((_, value, _) => value.Length <= maxLength).WithMessage((_, value) => $"{rb.GetPropertyDisplayName()} \"{value}\" must contain at most {maxLength} characters");
        }

        public static IRuleBuilderOptions<T, string> NameNotInUseWithMessage<T>(this IRuleBuilder<T, string> rb, IAccountClient accountClient)
        {
            ApiResponse<FindUserVM> userByNameResp = null;
            return rb.MustAsync(async (_, value, _, _) =>
            {
                userByNameResp = await accountClient.FindUserByNameAsync(value);
                if (userByNameResp.IsError && userByNameResp.Result == null)
                    return true;
                return false;
            }).WithMessage((_, value) => userByNameResp.IsError 
                ? userByNameResp.Message 
                : $"{rb.GetPropertyDisplayName()} \"{value}\" is already in use");
        }

        public static IRuleBuilderOptions<T, string> UserManagerCompliantWithMessage<T>(this IRuleBuilder<T, string> rb, IAccountClient accountClient)
        {
            //string value;
            //rb.Must(v => { value = v; return true; });

            ApiResponse<bool> userManagerComplianceResp = null;
            return rb.MustAsync(async (_, value, vc, _) =>
            {
                userManagerComplianceResp = await accountClient.CheckUserManagerComplianceAsync(vc.PropertyName, vc.DisplayName, value);
                //await Task.Delay(5000); // TODO: remove test
                return !userManagerComplianceResp.IsError && userManagerComplianceResp.Result;
            }).WithMessage((m, v) => userManagerComplianceResp.Message);
        }

        public static IRuleBuilderOptions<T, string> EmailAddressWithMessage<T>(this IRuleBuilder<T, string> rb)
        {
            ValidationContext<T> validationContext = null;
            return rb.Must((_, value, vc) =>
            {
                validationContext = vc;
                return value.IsEmailAddress();
            }).WithMessage((_, value) => $"{validationContext.DisplayName} \"{value}\" must be a valid email address");
        }

        public static IRuleBuilderOptions<T, string> EmailNotInUseWithMessage<T>(this IRuleBuilder<T, string> rb, IAccountClient accountClient)
        {
            ValidationContext<T> validationContext = null;
            ApiResponse<FindUserVM> userByEmailAsync = null;
            return rb.MustAsync(async (_, value, vc, _) =>
            {
                validationContext = vc;
                userByEmailAsync = await accountClient.FindUserByEmailAsync(value);
                if (userByEmailAsync.IsError && userByEmailAsync.Result == null)
                    return true;
                return false;
            }).WithMessage((_, value) => userByEmailAsync.IsError 
                ? userByEmailAsync.Message 
                : $"{validationContext.DisplayName} \"{value}\" is already in use");
        }

        public static IRuleBuilderOptions<T, string> EmailInUseWithMessage<T>(this IRuleBuilder<T, string> rb, IAccountClient accountClient)
        {
            ValidationContext<T> validationContext = null;
            ApiResponse<FindUserVM> userByEmailAsync = null;
            return rb.MustAsync(async (_, value, vc, _) =>
            {
                validationContext = vc;
                userByEmailAsync = await accountClient.FindUserByEmailAsync(value);
                if (userByEmailAsync.IsError)
                    return false;
                return userByEmailAsync.Result != null;
            }).WithMessage((_, value) => userByEmailAsync.IsError 
                ? userByEmailAsync.Message 
                : $"There is no User with {validationContext.DisplayName} \"{value}\" registered");
        }

        public static IRuleBuilderOptions<T, string> EqualWithMessage<T>(this IRuleBuilder<T, string> rb, Expression<Func<T, string>> predicate)
        {
            ValidationContext<T> validationContext = null;
            string otherPropDisplayName = null;
            string otherPropValue = null;
            return rb.Must((o, value, vc) =>
            {
                validationContext = vc;
                (_, _, otherPropValue, otherPropDisplayName) = predicate.GetModelAndProperty(o);
                return value != null && otherPropValue != null && value.EqualsInvariant(otherPropValue);
            }).WithMessage((_, value) => $"{validationContext.DisplayName} \"{value}\" must be equal to {otherPropDisplayName} \"{otherPropValue}\"");
        }

        public static IRuleBuilderOptions<T, string> Base58WithMessage<T>(this IRuleBuilder<T, string> rb)
        {
            return rb.Must((_, value, _) => value.IsBase58()).WithMessage($"{rb.GetPropertyDisplayName()} has to be a Base58 string");
        }
        
        public static IRuleBuilderOptions<T, string> CorrectConfirmationCodeWithMessage<T>(this IRuleBuilder<T, string> rb, IAccountClient accountClient)
        {
            ValidationContext<T> validationContext = null;
            ApiResponse<FindUserVM> userByActivationCode = null;
            return rb.MustAsync(async (_, value, vc, _) =>
            {
                validationContext = vc;
                userByActivationCode = await accountClient.FindUserByConfirmationCodeAsync(value);
                if (userByActivationCode.IsError)
                    return false;
                return userByActivationCode.Result != null;
            }).WithMessage((_, _) => $"{validationContext.DisplayName} is invalid");
        }
        
        public static IRuleBuilderOptions<T, string> AccountNotConfirmedWithMessage<T>(this IRuleBuilder<T, string> rb, IAccountClient accountClient)
        {
            ApiResponse<FindUserVM> userByActivationCode = null;
            return rb.MustAsync(async (_, value, _, _) =>
            {
                userByActivationCode = value.IsEmailAddress() 
                    ? await accountClient.FindUserByEmailAsync(value)
                    : await accountClient.FindUserByConfirmationCodeAsync(value);
                if (userByActivationCode.IsError)
                    return false;
                return userByActivationCode.Result?.IsConfirmed != true; // userByActivationCode.Result?.EmailActivationToken != null && userByActivationCode.Result?.IsConfirmed == false
            }).WithMessage((_, _) => "Account has already been confirmed");
        }
    }
}
