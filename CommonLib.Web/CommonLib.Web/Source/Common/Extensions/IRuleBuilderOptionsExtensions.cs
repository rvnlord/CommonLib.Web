using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
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

        public static IRuleBuilderOptions<T, string> NameNotInUseWithMessage<T>(this IRuleBuilder<T, string> rb, IAccountClient accountClient, IAccountManager accountManager)
        {
            ApiResponse<FindUserVM> userByNameResp = null;
            ApiResponse<FindUserVM> userByIdResp = null;
            return rb.MustAsync(async (model, value, _, _) =>
            {
                userByNameResp = accountClient is not null ? await accountClient.FindUserByNameAsync(value) : await accountManager.FindUserByNameAsync(value);
                if (userByNameResp.IsError)
                    return false;
                var id = model.GetProperty<Guid?>("Id");
                if (id != null && id != Guid.Empty)
                {
                    userByIdResp = accountClient is not null ? await accountClient.FindUserByIdAsync((Guid)id) : await accountManager.FindUserByIdAsync((Guid)id);
                    if (userByIdResp.IsError)
                        return false;
                }

                var userById = userByIdResp?.Result;
                var userByName = userByNameResp.Result;

                return userByName is null || userByName.UserName.EqualsIgnoreCase(userById?.UserName); // if there is no such user with supplied name or if its this user's current name
            }).WithMessage((_, value) => userByNameResp.IsError 
                ? userByNameResp.Message 
                : userByIdResp?.IsError == true
                    ? userByIdResp.Message 
                    : $"{rb.GetPropertyDisplayName()} \"{value}\" is already in use");
        }

        public static IRuleBuilderOptions<T, string> UserManagerCompliantWithMessage<T>(this IRuleBuilder<T, string> rb, IAccountClient accountClient, IAccountManager accountManager)
        {
            ApiResponse<bool> userManagerComplianceResp = null;
            return rb.MustAsync(async (_, value, vc, _) =>
            {
                userManagerComplianceResp = accountClient is not null ? await accountClient.CheckUserManagerComplianceAsync(vc.PropertyName, vc.DisplayName, value) : await accountManager.CheckUserManagerComplianceAsync(vc.PropertyName, vc.DisplayName, value);
                return !userManagerComplianceResp.IsError && userManagerComplianceResp.Result;
            }).WithMessage((_, _) => userManagerComplianceResp.Message);
        }
        
        public static IRuleBuilderOptions<T, string> UserManagerCompliantOrNullWithMessage<T>(this IRuleBuilder<T, string> rb, IAccountClient accountClient, IAccountManager accountManager)
        {
            ApiResponse<bool> userManagerComplianceResp = null;
            return rb.MustAsync(async (_, value, vc, _) =>
            {
                userManagerComplianceResp = accountClient is not null 
                    ? await accountClient.CheckUserManagerComplianceAsync(vc.PropertyName, vc.DisplayName, value)
                    : await accountManager.CheckUserManagerComplianceAsync(vc.PropertyName, vc.DisplayName, value);
                return value.IsNullOrWhiteSpace() || (!userManagerComplianceResp.IsError && userManagerComplianceResp.Result);
            }).WithMessage((_, _) => userManagerComplianceResp.Message);
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

        public static IRuleBuilderOptions<T, string> EmailNotInUseWithMessage<T>(this IRuleBuilder<T, string> rb, IAccountClient accountClient, IAccountManager accountManager)
        {
            ApiResponse<FindUserVM> userByEmailResp = null;
            ApiResponse<FindUserVM> userByIdResp = null;
            return rb.MustAsync(async (model, value, vc, _) =>
            {
                userByEmailResp = accountClient is not null ? await accountClient.FindUserByEmailAsync(value) : await accountManager.FindUserByEmailAsync(value);
                if (userByEmailResp.IsError)
                    return false;
                var id = model.GetProperty<Guid?>("Id");
                if (id != null && id != Guid.Empty)
                {
                    userByIdResp = accountClient is not null ? await accountClient.FindUserByIdAsync((Guid)id) : await accountManager.FindUserByIdAsync((Guid)id);
                    if (userByIdResp.IsError)
                        return false;
                }

                var userById = userByIdResp?.Result;
                var userByEmail = userByEmailResp.Result;
                
                return userByEmail is null || userByEmail.Email.EqualsIgnoreCase(userById?.Email);
            }).WithMessage((_, value) => userByEmailResp.IsError 
                ? userByEmailResp.Message 
                : userByIdResp?.IsError == true
                    ? userByIdResp.Message 
                    : $"{rb.GetPropertyDisplayName()} \"{value}\" is already in use");
        }

        public static IRuleBuilderOptions<T, string> EmailInUseWithMessage<T>(this IRuleBuilder<T, string> rb, IAccountClient accountClient, IAccountManager accountManager)
        {
            ValidationContext<T> validationContext = null;
            ApiResponse<FindUserVM> userByEmailAsync = null;
            return rb.MustAsync(async (_, value, vc, _) =>
            {
                validationContext = vc;
                userByEmailAsync = accountClient != null ? await accountClient.FindUserByEmailAsync(value) : await accountManager.FindUserByEmailAsync(value);
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
                return value.EqualsInvariant(otherPropValue);
            }).WithMessage((_, value) => $"{validationContext.DisplayName} \"{value}\" must be equal to {otherPropDisplayName} \"{otherPropValue}\"");
        }

        public static IRuleBuilderOptions<T, string> Base58WithMessage<T>(this IRuleBuilder<T, string> rb)
        {
            return rb.Must((_, value, _) => value is not null && value.IsBase58()).WithMessage($"{rb.GetPropertyDisplayName()} has to be a Base58 string");
        }
        
        public static IRuleBuilderOptions<T, string> CorrectConfirmationCodeWithMessage<T>(this IRuleBuilder<T, string> rb, IAccountClient accountClient, IAccountManager accountManager)
        {
            ValidationContext<T> validationContext = null;
            ApiResponse<FindUserVM> userByActivationCode;
            return rb.MustAsync(async (model, _, vc, _) =>
            {
                validationContext = vc;
                var confirmationCode = model.GetProperty<string>("ConfirmationCode").Base58ToUTF8();
                    userByActivationCode = accountClient is not null ? await accountClient.FindUserByConfirmationCodeAsync(confirmationCode) : await accountManager.FindUserByConfirmationCodeAsync(confirmationCode);
                if (userByActivationCode.IsError)
                    return false;
                return userByActivationCode.Result != null;
            }).WithMessage((_, _) => $"{validationContext.DisplayName} is invalid");
        }
        
        public static IRuleBuilderOptions<T, string> AccountNotConfirmedWithMessage<T>(this IRuleBuilder<T, string> rb, IAccountClient accountClient, IAccountManager accountManager)
        {
            ApiResponse<FindUserVM> user;
            return rb.MustAsync(async (model, _, _, _) =>
            {
                var id = model.GetPropertyOrNull<object>("Id") as Guid?;
                var email = model.GetPropertyOrNull<string>("Email");
                var userName = model.GetPropertyOrNull<string>("UserName");
                if (id is not null)
                    user = accountClient is not null ? await accountClient.FindUserByIdAsync((Guid)id) : await accountManager.FindUserByIdAsync((Guid)id);
                else if (email is not null)
                    user = accountClient is not null ? await accountClient.FindUserByEmailAsync(email) : await accountManager.FindUserByEmailAsync(email);
                else if (userName is not null)
                    user = accountClient is not null ? await accountClient.FindUserByNameAsync(userName) : await accountManager.FindUserByNameAsync(userName);
                else
                    return false;
                if (user.IsError)
                    return false;
                return user.Result?.IsConfirmed != true; // userByActivationCode.Result?.EmailActivationToken != null && userByActivationCode.Result?.IsConfirmed == false
            }).WithMessage((_, _) => "Account has already been confirmed");
        }

        public static IRuleBuilderOptions<T, string> AccountConfirmedWithMessage<T>(this IRuleBuilder<T, string> rb, IAccountClient accountClient, IAccountManager accountManager)
        {
            ApiResponse<FindUserVM> user;
            return rb.MustAsync(async (model, value, _, _) =>
            {
                if (value.IsNullOrWhiteSpace())
                    return false;
                user = accountClient is not null ? await accountClient.FindUserByEmailAsync(model.GetProperty<string>("Email")) :  await accountManager.FindUserByEmailAsync(model.GetProperty<string>("Email"));
                if (user.IsError)
                    return false;
                return user.Result?.IsConfirmed == true;
            }).WithMessage((_, _) => "Account has not been confirmed yet");
        }

        public static IRuleBuilderOptions<T, string> CorrectResetPasswordCodeWithMessage<T>(this IRuleBuilder<T, string> rb, IAccountClient accountClient, IAccountManager accountManager)
        {
            ValidationContext<T> validationContext = null;
            return rb.MustAsync(async (model, value, vc, _) =>
            {
                validationContext = vc;
                var checkResetPasswordCodeResp = accountClient is not null 
                    ? await accountClient.CheckUserResetPasswordCodeAsync(new CheckResetPasswordCodeUserVM
                    {
                        UserName = model.GetProperty<string>("UserName"),
                        CheckResetPasswordCode = value
                    }) 
                    : await accountManager.CheckUserResetPasswordCodeAsync(new CheckResetPasswordCodeUserVM
                    {
                        UserName = model.GetProperty<string>("UserName"),
                        CheckResetPasswordCode = value
                    });
                if (checkResetPasswordCodeResp.IsError)
                    return false;
                return checkResetPasswordCodeResp.Result;
            }).WithMessage((_, _) => $"{validationContext.DisplayName} is invalid");
        }

        public static IRuleBuilderOptions<T, string> IsNotExistingPasswordWithMessage<T>(this IRuleBuilder<T, string> rb, IAccountClient accountClient, IAccountManager accountManager)
        {
            return rb.MustAsync(async (model, value, _, _) =>
            {
                var checkPasswordResp = accountClient is not null 
                    ? await accountClient.CheckUserPasswordAsync(new CheckPasswordUserVM
                    {
                        Id = model.GetProperty<Guid>("Id"),
                        Password = value
                    }) 
                    : await accountManager.CheckUserPasswordAsync(new CheckPasswordUserVM
                    {
                        Id = model.GetProperty<Guid>("Id"),
                        Password = value
                    });
                if (checkPasswordResp.IsError)
                    return false;
                return !checkPasswordResp.Result;
            }).WithMessage((_, _) => $"Your new password can't be the same as your old password");
        }

        public static IRuleBuilderOptions<T, string> IsExistingPasswordWithMessage<T>(this IRuleBuilder<T, string> rb, IAccountClient accountClient, IAccountManager accountManager)
        {
            ApiResponse<bool> checkPasswordResp = null;
            ValidationContext<T> validationContext = null;
            return rb.MustAsync(async (model, value, vc, _) =>
            {
                validationContext = vc;
                checkPasswordResp = accountClient is not null
                    ? await accountClient.CheckUserPasswordAsync(new CheckPasswordUserVM
                    {
                        Id = model.GetProperty<Guid>("Id"),
                        Password = value
                    })
                    : await accountManager.CheckUserPasswordAsync(new CheckPasswordUserVM
                    {
                        Id = model.GetProperty<Guid>("Id"),
                        Password = value
                    });
                return !checkPasswordResp.IsError && checkPasswordResp.Result;
            }).WithMessage((_, _) => checkPasswordResp.IsError 
                ? checkPasswordResp.Message 
                : $"{validationContext.DisplayName} is Incorrect");
        }
        
        public static IRuleBuilderOptions<T, string> RequiredIfHasPasswordWithMessage<T>(this IRuleBuilder<T, string> rb)
        {
            ValidationContext<T> validationContext = null;
            return rb.Must((model, value, vc) =>
            {
                validationContext = vc;
                var hasPassword = model.GetProperty<bool>("HasPassword");
                return hasPassword && !value.IsNullOrWhiteSpace() || !hasPassword;
            }).WithMessage((_, _) => $"{validationContext.DisplayName} is required");
        }
        
    }
}
