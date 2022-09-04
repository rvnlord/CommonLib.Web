using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Services.Account.Interfaces;
using CommonLib.Web.Source.ViewModels.Account;
using FluentValidation;

namespace CommonLib.Web.Source.Validators.Account
{
    public class ResendConfirmationEmailUserVMValidator : AbstractValidator<ResendConfirmationEmailUserVM>
    {
        public ResendConfirmationEmailUserVMValidator(IAccountClient accountClient)
        {
            ClassLevelCascadeMode = CascadeMode.Stop;
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(m => m.Email)
                .RequiredWithMessage()
                .EmailAddressWithMessage()
                .EmailInUseWithMessage(accountClient)
                .AccountNotConfirmedWithMessage(accountClient);
        }
    }
}
