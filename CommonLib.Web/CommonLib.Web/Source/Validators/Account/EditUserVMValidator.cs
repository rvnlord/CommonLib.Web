using System;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils;
using CommonLib.Web.Source.Services.Account;
using CommonLib.Web.Source.Services.Account.Interfaces;
using CommonLib.Web.Source.ViewModels.Account;
using FluentValidation;
using SimpleInjector;

namespace CommonLib.Web.Source.Validators.Account
{
    public class EditUserVMValidator : AbstractValidator<EditUserVM>, IDisposable
    {
        private Scope _accountClientScope;
        private Scope _accountManagerScope;

        public IAccountClient AccountClient { get; set; }
        public IAccountManager AccountManager { get; set; }

        public EditUserVMValidator()
        {
            Initialize();
        }

        public EditUserVMValidator(IAccountManager accountManager)
        {
            Initialize(accountManager);
        }

        private void Initialize(IAccountManager accountManager = null)
        {
            (AccountClient, _accountClientScope) = WebUtils.GetScopedServiceOrNull<IAccountClient>();
            if (accountManager is null)
                (AccountManager, _accountManagerScope) = WebUtils.GetScopedServiceOrNull<IAccountManager>();

            AccountManager = accountManager;

            ClassLevelCascadeMode = CascadeMode.Stop;
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(m => m.UserName)
                .RequiredWithMessage()
                .MinLengthWithMessage(3)
                .MaxLengthWithMessage(25)
                .UserManagerCompliantWithMessage(AccountClient, AccountManager)
                .NameNotInUseWithMessage(AccountClient, AccountManager);

            RuleFor(m => m.Email)
                .RequiredWithMessage()
                .EmailAddressWithMessage()
                .EmailNotInUseWithMessage(AccountClient, AccountManager);

            RuleFor(m => m.OldPassword)
                .RequiredIfHasPasswordWithMessage()
                .IsExistingPasswordWithMessage(AccountClient, AccountManager);

            RuleFor(m => m.NewPassword)
                .UserManagerCompliantOrNullWithMessage(AccountClient, AccountManager)
                .IsNotExistingPasswordWithMessage(AccountClient, AccountManager);

            RuleFor(m => m.ConfirmNewPassword)
                .EqualWithMessage(m => m.NewPassword);
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

        ~EditUserVMValidator() {
            Dispose(false);
        }
    }
}
