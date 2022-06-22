using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Services.Account.Interfaces;
using CommonLib.Web.Source.ViewModels.Account;
using FluentValidation;

namespace CommonLib.Web.Source.Validators.Account
{
    public class ConfirmUserVMValidator : AbstractValidator<ConfirmUserVM>
    {
        public ConfirmUserVMValidator(IAccountClient accountClient)
        {
            CascadeMode = CascadeMode.Stop;

            RuleFor(m => m.Email)
                .RequiredWithMessage()
                .EmailAddressWithMessage()
                .EmailInUseWithMessage(accountClient);
            
            RuleFor(m => m.ConfirmationCode)
                .RequiredWithMessage()
                .Base58WithMessage()
                .AccountNotConfirmedWithMessage(accountClient)
                .CorrectConfirmationCodeWithMessage(accountClient);
        }
    }
}
