using System;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils;
using CommonLib.Web.Source.Services.Account.Interfaces;
using CommonLib.Web.Source.ViewModels.Admin;
using FluentValidation;
using SimpleInjector;

namespace CommonLib.Web.Source.Validators.Admin
{
    public class AccountEditRoleVMValidator : AbstractValidator<AdminEditRoleVM>, IDisposable
    {
        private Scope _accountClientScope;
        private Scope _accountManagerScope;

        public IAccountClient AccountClient { get; set; }
        public IAccountManager AccountManager { get; set; }

        public AccountEditRoleVMValidator()
        {
            Initialize();
        }

        public AccountEditRoleVMValidator(IAccountManager accountManager)
        {
            Initialize(accountManager);
        }

        private void Initialize(IAccountManager accountManager = null)
        {
            (AccountClient, _accountClientScope) = WebUtils.GetScopedServiceOrNull<IAccountClient>();
            if (accountManager is null)
                (AccountManager, _accountManagerScope) = WebUtils.GetScopedServiceOrNull<IAccountManager>();

            AccountManager = accountManager;

            RuleFor(m => m.Name)
                .RequiredWithMessage()
                .MinLengthWithMessage(3)
                .MaxLengthWithMessage(25)
                .AlphaNumericWithMessage()
                .RoleNotInUseWithMessage(AccountClient, AccountManager);
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

        ~AccountEditRoleVMValidator() {
            Dispose(false);
        }
    }
}