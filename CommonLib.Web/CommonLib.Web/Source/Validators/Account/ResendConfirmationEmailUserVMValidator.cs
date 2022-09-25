using System;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils;
using CommonLib.Web.Source.Services.Account.Interfaces;
using CommonLib.Web.Source.ViewModels.Account;
using FluentValidation;
using SimpleInjector;

namespace CommonLib.Web.Source.Validators.Account
{
    public class ForgotPasswordUserVMValidator : AbstractValidator<ForgotPasswordUserVM>, IDisposable
    {
        private readonly Scope _accountClientScope;
        private readonly Scope _accountManagerScope;

        public ForgotPasswordUserVMValidator()
        {
            (var accountClient, _accountClientScope) = WebUtils.GetScopedServiceOrNull<IAccountClient>();
            (var accountManager, _accountManagerScope) = WebUtils.GetScopedServiceOrNull<IAccountManager>();

            ClassLevelCascadeMode = CascadeMode.Stop;
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(m => m.Email)
                .RequiredWithMessage()
                .EmailAddressWithMessage()
                .EmailInUseWithMessage(accountClient, accountManager)
                .AccountConfirmedWithMessage(accountClient, accountManager);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            _accountClientScope?.Dispose();
            _accountManagerScope?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ForgotPasswordUserVMValidator() {
            Dispose(false);
        }
    }
}
