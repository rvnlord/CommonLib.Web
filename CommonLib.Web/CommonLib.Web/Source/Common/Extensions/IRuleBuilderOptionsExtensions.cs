using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Web.Source.Models;
using CommonLib.Web.Source.Services.Account.Interfaces;
using CommonLib.Web.Source.ViewModels.Account;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Source.Models;
using CommonLib.Web.Source.Services.Admin.Interfaces;
using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using MoreLinq.Extensions;
using WebSocketSharp;
using CommonLib.Web.Source.Services.Admin;

namespace CommonLib.Web.Source.Common.Extensions
{
    public static class IRuleBuilderOptionsExtensions
    {
        public static IRuleBuilderOptions<T, string> RequiredWithMessage<T>(this IRuleBuilder<T, string> rb)
        {
            return rb.Must((_, value, _) => !value.IsNullOrWhiteSpace()).WithMessage($"{rb.GetPropertyDisplayName()} is required");
        }

        public static IRuleBuilderOptions<TModel, TProperty> RequiredWithMessage<TModel, TProperty>(this IRuleBuilder<TModel, TProperty> rb)
        {
            return rb.Must((_, value, _) => value is not null).WithMessage($"{rb.GetPropertyDisplayName()} is required");
        }

        public static IRuleBuilderOptions<TModel, TProperty> EqualWithMessage<TModel, TProperty>(this IRuleBuilder<TModel, TProperty> rb, TProperty other)
        {
            return rb.Must((_, value, _) => value.Equals(other)).WithMessage($"{rb.GetPropertyDisplayName()} has to equal to \"{other}\"");
        }

        public static IRuleBuilderOptions<TModel, TProperty> NotEqualWithMessage<TModel, TProperty>(this IRuleBuilder<TModel, TProperty> rb, TProperty other)
        {
            return rb.Must((_, value, _) => !value.Equals(other)).WithMessage($"{rb.GetPropertyDisplayName()} can't equal to \"{other}\"");
        }

        public static IRuleBuilderOptions<T, string> MinLengthWithMessage<T>(this IRuleBuilder<T, string> rb, int minLength)
        {
            return rb.Must((_, value, _) => value?.Length >= minLength).WithMessage((_, value) => $"{rb.GetPropertyDisplayName()} \"{value}\" must contain at least {minLength} characters");
        }

        public static IRuleBuilderOptions<T, string> MaxLengthWithMessage<T>(this IRuleBuilder<T, string> rb, int maxLength)
        {
            return rb.Must((_, value, _) => value?.Length <= maxLength).WithMessage((_, value) => $"{rb.GetPropertyDisplayName()} \"{value}\" must contain at most {maxLength} characters");
        }

        public static IRuleBuilderOptions<T, string> AlphaNumericWithMessage<T>(this IRuleBuilder<T, string> rb)
        {
            return rb.Must((_, value, _) => value is not null && Regex.IsMatch(value, "^[a-zA-Z0-9 ]*$")).WithMessage((_, value) => $"{rb.GetPropertyDisplayName()} \"{value}\" must only contain alphanumeric characters");
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
                : $"There is no DbUser with {validationContext.DisplayName} \"{value}\" registered");
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

        public static IRuleBuilderOptions<TModel, FileDataList> FileSizeWithMessage<TModel>(this IRuleBuilder<TModel, FileDataList> rb, Expression<Func<FileSize, bool>> fdCondition)
        {
            ValidationContext<TModel> validationContext;
            string displayName = null;
            var conditionString = "match the condition";
            return rb.Must((_, value, vc) =>
            {
                validationContext = vc;
                displayName = validationContext.DisplayName ?? validationContext.GetField("_parentContext")?.GetProperty<string>("DisplayName");
                var op = (fdCondition.Body as BinaryExpression)?.NodeType;
                var exprSizeCtor = (fdCondition.Body as BinaryExpression)?.Right as NewExpression;

                var exprSizeParam = Expression.Parameter(typeof(double), "size");
                var exprSuffixParam = Expression.Parameter(typeof(FileSizeSuffix), "suffix");
                FileSize? size = null;
                if (exprSizeCtor is not null)
                {
                    var sizeD = (exprSizeCtor.Arguments[0] as ConstantExpression)?.Value?.ToDoubleN();
                    var suffix = (exprSizeCtor.Arguments[1] as ConstantExpression)?.Value?.ToEnumN<FileSizeSuffix>();
                    var exprSizeCompiled = Expression.Lambda<Func<double, FileSizeSuffix, FileSize>>(exprSizeCtor, exprSizeParam, exprSuffixParam).Compile();
                    if (sizeD is not null && suffix is not null)
                        size = exprSizeCompiled((double)sizeD, (FileSizeSuffix)suffix);
                }

                if (op is not null && size is not null)
                {
                    if (op == ExpressionType.LessThan)
                        conditionString = $"be < {size}";
                    else if (op == ExpressionType.LessThanOrEqual)
                        conditionString = $"be <= {size}";
                    else if (op == ExpressionType.GreaterThan)
                        conditionString = $"be > {size}";
                    else if (op == ExpressionType.GreaterThanOrEqual)
                        conditionString = $"be >= {size}";
                }
           
                var fdConditionCompiled = fdCondition.Compile();
                value.ForEach(fd => fd.IsFileSizeValid = fdConditionCompiled.Invoke(fd.TotalSize));
                return value.All(fd => fd.IsFileSizeValid);
            }).WithMessage((_, _) => $"Each of the {displayName}' size must {conditionString}");
        }

        public static IRuleBuilderOptions<TModel, FileDataList> FileExtensionWithMessage<TModel>(this IRuleBuilder<TModel, FileDataList> rb, params string[] extensions)
        {
            ValidationContext<TModel> validationContext;
            string displayName = null;
            string[] expectedExtensions = null;
            return rb.Must((_, value, vc) =>
            {
                validationContext = vc;
                displayName = validationContext.DisplayName ?? validationContext.GetField("_parentContext")?.GetProperty<string>("DisplayName");
                expectedExtensions = extensions.Select(ext => ext.AfterFirstOrWhole(".").ToLowerInvariant()).ToArray();
                value.ForEach(fd => fd.IsExtensionValid = fd.Extension.ToLowerInvariant().In(expectedExtensions));
                return value.All(fd => fd.IsExtensionValid);
            }).WithMessage((_, _) => $"{displayName} must have one of the following extensions: {expectedExtensions?.Select(ext => $"\".{ext}\"").JoinAsString(", ")}");
        }
        
        public static IRuleBuilderOptions<TModel, FileDataList> FilesUploadedWithMessage<TModel>(this IRuleBuilder<TModel, FileDataList> rb, int? numberOfFilesThatShouldBeUploaded = null)
        {
            string displayName = null;
            return rb.Must((_, value, vc) =>
            {
                displayName = vc.DisplayName ?? vc.GetField("_parentContext")?.GetProperty<string>("DisplayName");
                var actualUploadedFilesCount = value.Count(fd => fd.Status == UploadStatus.Finished || !fd.ValidateUploadStatus);
                var expectedUploadedFilesCount = numberOfFilesThatShouldBeUploaded ?? value.Count;
                return actualUploadedFilesCount == expectedUploadedFilesCount;
            }).WithMessage((_, _) => numberOfFilesThatShouldBeUploaded is null 
                ? $"All \"{displayName}\" have to be uploaded"
                : $"At least {numberOfFilesThatShouldBeUploaded} of the \"{displayName}\" have to be uploaded");
        }

        public static IRuleBuilderOptions<TModel, FileDataList> NoFileIsUploadingWithMessage<TModel>(this IRuleBuilder<TModel, FileDataList> rb)
        {
            string displayName = null;
            return rb.Must((_, value, vc) =>
            {
                displayName = vc.DisplayName ?? vc.GetField("_parentContext")?.GetProperty<string>("DisplayName");
                return value.All(fd => fd.Status != UploadStatus.Uploading || !fd.ValidateUploadStatus);
            }).WithMessage((_, _) => $"\"{displayName}\" can't be still uploading");
        }

        public static IRuleBuilderOptions<TModel, FileDataList> PotentialAvatarsNotInUseWithMessage<TModel>(this IRuleBuilder<TModel, FileDataList> rb, IAccountClient accountClient, IAccountManager accountManager)
        {
            ApiResponse<FileDataList> avatarsInUseResp = null;
            string displayName = null;
            return rb.MustAsync(async (_, value, vc, _) =>
            {
                displayName = vc.DisplayName ?? vc.GetField("_parentContext")?.GetProperty<string>("DisplayName");
                avatarsInUseResp = accountClient is not null ? await accountClient.FindAvatarsInUseAsync(false) : await accountManager.FindAvatarsInUseAsync(false);
                if (avatarsInUseResp.IsError)
                    return false;
                
                value.ForEach(fd => fd.IsAlreadyInUse = fd.In(avatarsInUseResp.Result));
                return !avatarsInUseResp.Result.Intersect(value).Any();
            }).WithMessage((_, _) => avatarsInUseResp.IsError ? avatarsInUseResp.Message : $"Some of the \"{displayName}\" are already in use");
        }

        public static IRuleBuilderOptions<T, string> RoleNotInUseWithMessage<T>(this IRuleBuilder<T, string> rb, IAccountClient accountClient, IAccountManager accountManager, IAdminClient adminClient, IAdminManager adminManager)
        {
            ApiResponse<FindRoleVM> roleByNameResp;
            ApiResponse<FindRoleVM> roleByIdResp;
            string message = null;
            return rb.MustAsync(async (model, value, _, _) =>
            {
                roleByNameResp = accountClient is not null ? await accountClient.FindRoleByNameAsync(value) : await accountManager.FindRoleByNameAsync(value);
                if (roleByNameResp.IsError)
                {
                    message = roleByNameResp.Message;
                    return false;
                }

                var roleByName = roleByNameResp.Result;
                if (roleByName is null)
                    return true;

                var id = model.GetProperty<Guid>("Id");
                if (id == default)
                {
                    message = $"{rb.GetPropertyDisplayName()} \"{value}\" is already in use";
                    return false;
                }

                roleByIdResp = adminClient is not null ? await adminClient.FindRoleByIdAsync(id) : await adminManager.FindRoleByIdAsync(id);
                if (roleByIdResp.IsError)
                {
                    message = roleByIdResp.Message;
                    return false;
                }

                var roleByid = roleByIdResp.Result;
                if (roleByid is null || !roleByid.Name.EqualsIgnoreCase(roleByName.Name))
                {
                    message = $"{rb.GetPropertyDisplayName()} \"{value}\" is already in use";
                    return false;
                }
                
                return true;
            }).WithMessage((_, _) => message);
        }

        public static IRuleBuilderOptions<T, string> ClaimNotInUseWithMessage<T>(this IRuleBuilder<T, string> rb, IAccountClient accountClient, IAccountManager accountManager)
        {
            ApiResponse<FindClaimVM> claimByNameResp;
            ApiResponse<FindClaimVM> claimByOriginalNameResp;
            string message = null;
            return rb.MustAsync(async (model, value, _, _) =>
            {
                claimByNameResp = accountClient is not null ? await accountClient.FindClaimByNameAsync(value) : await accountManager.FindClaimByNameAsync(value);
                if (claimByNameResp.IsError)
                {
                    message = claimByNameResp.Message;
                    return false;
                }

                var claimByName = claimByNameResp.Result;
                if (claimByName is null)
                    return true;

                var originalName = model.GetProperty<string>("OriginalName");
                if (originalName is null)
                {
                    message = $"{rb.GetPropertyDisplayName()} \"{value}\" is already in use";
                    return false;
                }
                claimByOriginalNameResp = accountClient is not null ? await accountClient.FindClaimByNameAsync(originalName) : await accountManager.FindClaimByNameAsync(originalName);
                if (claimByOriginalNameResp.IsError)
                {
                    message = claimByOriginalNameResp.Message;
                    return false;
                }

                var claimByid = claimByOriginalNameResp.Result;
                if (claimByid is null || !claimByid.Name.EqualsIgnoreCase(claimByName.Name))
                {
                    message = $"{rb.GetPropertyDisplayName()} \"{value}\" is already in use";
                    return false;
                }
                
                return true;
            }).WithMessage((_, _) => message);
        }

        public static IRuleBuilderOptions<T, decimal?> BetweenWithMessage<T>(this IRuleBuilder<T, decimal?> rb, decimal? minValue, decimal? maxValue)
        {
            return rb.Must((_, value, _) => value >= minValue && value <= maxValue).WithMessage((_, value) => $"{rb.GetPropertyDisplayName()} \"{value}\" must be between \"{minValue:0.00}\" and \"{maxValue:0.00}\"");
        }

        public static IRuleBuilderOptions<T, DateTime?> BetweenWithMessage<T>(this IRuleBuilder<T, DateTime?> rb,  DateTime? minValue,  DateTime? maxValue)
        {
            return rb.Must((_, value, _) => value >= minValue && value <= maxValue).WithMessage((_, value) => $"{rb.GetPropertyDisplayName()} \"{$"{value:dd-MM-yyyy HH:mm:ss}".BeforeLastOrWhole(" 00:00:00")}\" must be between \"{$"{minValue:dd-MM-yyyy HH:mm:ss}".BeforeLastOrWhole(" 00:00:00")}\" and \"{$"{maxValue:dd-MM-yyyy HH:mm:ss}".BeforeLastOrWhole(" 00:00:00")}\"");
        }

        public static IRuleBuilderOptions<TModel, TProperty> WithDisplayName<TModel, TProperty>(this IRuleBuilder<TModel, TProperty> rb, Expression<Func<TModel, TProperty>> propertySelector) where TModel : new()
        {
            DefaultValidatorOptions.Configurable(rb).SetDisplayName(new TModel().GetPropertyDisplayName(propertySelector));
            return (IRuleBuilderOptions<TModel, TProperty>)rb;
        }
    }
}
