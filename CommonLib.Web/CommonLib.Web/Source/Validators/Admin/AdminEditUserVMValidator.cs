using System;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils;
using CommonLib.Web.Source.Services.Account.Interfaces;
using CommonLib.Web.Source.Validators.Upload;
using CommonLib.Web.Source.ViewModels.Admin;
using FluentValidation;
using SimpleInjector;

namespace CommonLib.Web.Source.Validators.Admin
{
    public class AdminEditUserVMValidator : AbstractValidator<AdminEditUserVM>, IDisposable
    {
        private Scope _accountClientScope;
        private Scope _accountManagerScope;

        public IAccountClient AccountClient { get; set; }
        public IAccountManager AccountManager { get; set; }

        public AdminEditUserVMValidator()
        {
            Initialize();
        }

        public AdminEditUserVMValidator(IAccountManager accountManager)
        {
            Initialize(accountManager);
        }

        private void Initialize(IAccountManager accountManager = null)
        {
            (AccountClient, _accountClientScope) = WebUtils.GetScopedServiceOrNull<IAccountClient>();
            if (accountManager is null)
                (AccountManager, _accountManagerScope) = WebUtils.GetScopedServiceOrNull<IAccountManager>();

            AccountManager = accountManager;

            ClassLevelCascadeMode = CascadeMode.Continue;
            RuleLevelCascadeMode = CascadeMode.Continue;

            RuleFor(m => m.UserName)
                .RequiredWithMessage()
                .MinLengthWithMessage(3)
                .MaxLengthWithMessage(25)
                .UserManagerCompliantWithMessage(AccountClient, AccountManager)
                .NameNotInUseWithMessage(AccountClient, AccountManager);

            RuleFor(m => m.PotentialAvatars)
                .SetValidator(new AvatarValidator());

            RuleFor(m => m.Email)
                .RequiredWithMessage()
                .EmailAddressWithMessage()
                .EmailNotInUseWithMessage(AccountClient, AccountManager);
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

        ~AdminEditUserVMValidator() {
            Dispose(false);
        }
    }
}
