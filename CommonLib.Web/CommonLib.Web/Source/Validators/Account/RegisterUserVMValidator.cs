using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Services.Account.Interfaces;
using CommonLib.Web.Source.ViewModels.Account;
using FluentValidation;

namespace CommonLib.Web.Source.Validators.Account
{
    public class RegisterUserVMValidator : AbstractValidator<RegisterUserVM>
    {
        public RegisterUserVMValidator(IAccountClient accountClient)
        {
            CascadeMode = CascadeMode.Stop;
            
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

            RuleFor(m => m.Password)
                .RequiredWithMessage()
                .UserManagerCompliantWithMessage(accountClient);

            RuleFor(m => m.ConfirmPassword)
                .RequiredWithMessage()
                .EqualWithMessage(m => m.Password);
        }
    }
}
