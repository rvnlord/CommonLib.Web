using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Services.Account.Interfaces;
using CommonLib.Web.Source.ViewModels.Account;
using FluentValidation;

namespace CommonLib.Web.Source.Validators.Account
{
    public class ResetPasswordVMValidator : AbstractValidator<ResetPasswordUserVM>
    {
        public ResetPasswordVMValidator(IAccountClient accountClient)
        {
            ClassLevelCascadeMode = CascadeMode.Stop;
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(m => m.Email)
                .RequiredWithMessage()
                .EmailAddressWithMessage()
                .EmailInUseWithMessage(accountClient);

            RuleFor(m => m.ResetPasswordCode)
                .RequiredWithMessage()
                .Base58WithMessage()
                .CorrectResetPasswordCodeWithMessage(accountClient)
                .AccountConfirmedWithMessage(accountClient);
                
            RuleFor(m => m.Password)
                .RequiredWithMessage()
                .UserManagerCompliantWithMessage(accountClient)
                .IsNotExistingPasswordWithMessage(accountClient);

            RuleFor(m => m.ConfirmPassword)
                .RequiredWithMessage()
                .EqualWithMessage(m => m.Password);
        }
    }
}
