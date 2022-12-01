using System;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils;
using CommonLib.Web.Source.Services.Account.Interfaces;
using CommonLib.Web.Source.Services.Admin.Interfaces;
using CommonLib.Web.Source.ViewModels.Admin;
using FluentValidation;
using SimpleInjector;

namespace CommonLib.Web.Source.Validators.Admin
{
    public class AdminEditClaimVMValidator : AbstractValidator<AdminEditClaimVM>, IDisposable
    {
        private Scope _adminClientScope;
        private Scope _adminManagerScope;
        private Scope _accountClientScope;
        private Scope _accountManagerScope;

        public IAdminClient AdminClient { get; set; }
        public IAdminManager AdminManager { get; set; }
        public IAccountClient AccountClient { get; set; }
        public IAccountManager AccountManager { get; set; }

        public AdminEditClaimVMValidator()
        {
            Initialize();
        }

        public AdminEditClaimVMValidator(IAdminManager adminManager, IAccountManager accountManager)    
        {
            Initialize(adminManager, accountManager);
        }

        private void Initialize(IAdminManager adminManager = null, IAccountManager accountManager = null)
        {
            (AdminClient, _adminClientScope) = WebUtils.GetScopedServiceOrNull<IAdminClient>();
            if (adminManager is null)
                (AdminManager, _adminManagerScope) = WebUtils.GetScopedServiceOrNull<IAdminManager>();
            (AccountClient, _accountClientScope) = WebUtils.GetScopedServiceOrNull<IAccountClient>();
            if (adminManager is null)
                (AccountManager, _accountManagerScope) = WebUtils.GetScopedServiceOrNull<IAccountManager>();

            AdminManager = adminManager;

            RuleFor(m => m.Name)
                .RequiredWithMessage()
                .MinLengthWithMessage(3)
                .MaxLengthWithMessage(25)
                .AlphaNumericWithMessage()
                .ClaimNotInUseWithMessage(AccountClient, AccountManager);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            _adminClientScope?.Dispose();
            _adminManagerScope?.Dispose();
            _accountClientScope?.Dispose();
            _accountManagerScope?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~AdminEditClaimVMValidator() {
            Dispose(false);
        }
    }
}
