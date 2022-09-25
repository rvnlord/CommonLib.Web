using System;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils;
using CommonLib.Web.Source.Services.Account.Interfaces;
using CommonLib.Web.Source.ViewModels.Account;
using FluentValidation;
using SimpleInjector;

namespace CommonLib.Web.Source.Validators.Account
{
    public class RegisterUserVMValidator : AbstractValidator<RegisterUserVM>, IDisposable
    {
        private readonly Scope _accountClientScope;
        private readonly Scope _accountManagerScope;

        public RegisterUserVMValidator()
        {
            (var accountClient, _accountClientScope) = WebUtils.GetScopedServiceOrNull<IAccountClient>();
            (var accountManager, _accountManagerScope) = WebUtils.GetScopedServiceOrNull<IAccountManager>();

            ClassLevelCascadeMode = CascadeMode.Stop;
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(m => m.UserName)
                .RequiredWithMessage()
                .MinLengthWithMessage(3)
                .MaxLengthWithMessage(25)
                .NameNotInUseWithMessage(accountClient, accountManager)
                .UserManagerCompliantWithMessage(accountClient, accountManager);
            
            RuleFor(m => m.Email)
                .RequiredWithMessage()
                .EmailAddressWithMessage()
                .EmailNotInUseWithMessage(accountClient, accountManager);

            RuleFor(m => m.Password)
                .RequiredWithMessage()
                .UserManagerCompliantWithMessage(accountClient, accountManager);

            RuleFor(m => m.ConfirmPassword)
                .RequiredWithMessage()
                .EqualWithMessage(m => m.Password);
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

        ~RegisterUserVMValidator() {
            Dispose(false);
        }
    }
}
