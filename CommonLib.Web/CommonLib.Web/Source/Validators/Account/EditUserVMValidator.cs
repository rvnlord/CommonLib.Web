using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Services.Account.Interfaces;
using CommonLib.Web.Source.ViewModels.Account;
using FluentValidation;

namespace CommonLib.Web.Source.Validators.Account
{
    public class EditUserVMValidator : AbstractValidator<EditUserVM>
    {
        public EditUserVMValidator(IAccountClient accountClient)
        {
            ClassLevelCascadeMode = CascadeMode.Stop;
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(m => m.UserName)
                .RequiredWithMessage()
                .MinLengthWithMessage(3)
                .MaxLengthWithMessage(25)
                .NameNotInUseWithMessage(accountClient)
                .UserManagerCompliantWithMessage(accountClient);

            RuleFor(m => m.Email)
                .RequiredWithMessage()
                .EmailAddressWithMessage()
                .EmailNotInUseWithMessage(accountClient);

            RuleFor(m => m.OldPassword)
                .RequiredWithMessage()
                .IsExistingPasswordWithMessage(accountClient);

            RuleFor(m => m.NewPassword)
                .UserManagerCompliantOrNullWithMessage(accountClient)
                .IsNotExistingPasswordWithMessage(accountClient);

            RuleFor(m => m.ConfirmNewPassword)
                .EqualWithMessage(m => m.NewPassword);
        }
    }
}
