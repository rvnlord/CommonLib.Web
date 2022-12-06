using System;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils;
using CommonLib.Web.Source.Services.Account.Interfaces;
using CommonLib.Web.Source.Services.Admin;
using CommonLib.Web.Source.Services.Admin.Interfaces;
using CommonLib.Web.Source.ViewModels.Admin;
using FluentValidation;
using SimpleInjector;

namespace CommonLib.Web.Source.Validators.Admin
{
    public class AdminEditRoleVMValidator : AbstractValidator<AdminEditRoleVM>, IDisposable
    {
        private Scope _accountClientScope;
        private Scope _accountManagerScope;
        private Scope _adminClientScope;
        private Scope _adminManagerScope;

        public IAccountClient AccountClient { get; set; }
        public IAccountManager AccountManager { get; set; }
        public IAdminClient AdminClient { get; set; }
        public IAdminManager AdminManager { get; set; }

        public AdminEditRoleVMValidator()
        {
            Initialize();
        }

        public AdminEditRoleVMValidator(IAccountManager accountManager, IAdminManager adminManager)
        {
            Initialize(accountManager, adminManager);
        }

        private void Initialize(IAccountManager accountManager = null, IAdminManager adminManager = null)
        {
            (AccountClient, _accountClientScope) = WebUtils.GetScopedServiceOrNull<IAccountClient>();
            if (accountManager is null)
                (AccountManager, _accountManagerScope) = WebUtils.GetScopedServiceOrNull<IAccountManager>();
            AccountManager = accountManager;
            (AdminClient, _adminClientScope) = WebUtils.GetScopedServiceOrNull<IAdminClient>();
            if (adminManager is null)
                (AdminManager, _adminManagerScope) = WebUtils.GetScopedServiceOrNull<IAdminManager>();
            AdminManager = adminManager;

            RuleFor(m => m.Name)
                .RequiredWithMessage()
                .MinLengthWithMessage(3)
                .MaxLengthWithMessage(25)
                .AlphaNumericWithMessage()
                .RoleNotInUseWithMessage(AccountClient, AccountManager, AdminClient, AdminManager);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            _accountClientScope?.Dispose();
            _accountManagerScope?.Dispose();
            _accountClientScope?.Dispose();
            _adminManagerScope?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~AdminEditRoleVMValidator() {
            Dispose(false);
        }
    }
}