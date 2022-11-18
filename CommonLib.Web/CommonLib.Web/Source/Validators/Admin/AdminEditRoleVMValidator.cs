using System;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils;
using CommonLib.Web.Source.Services.Admin.Interfaces;
using CommonLib.Web.Source.ViewModels.Admin;
using FluentValidation;
using SimpleInjector;

namespace CommonLib.Web.Source.Validators.Admin
{
    public class AdminEditRoleVMValidator : AbstractValidator<AdminEditRoleVM>, IDisposable
    {
        private Scope _adminClientScope;
        private Scope _adminManagerScope;

        public IAdminClient AdminClient { get; set; }
        public IAdminManager AdminManager { get; set; }

        public AdminEditRoleVMValidator()
        {
            Initialize();
        }

        public AdminEditRoleVMValidator(IAdminManager adminManager)
        {
            Initialize(adminManager);
        }

        private void Initialize(IAdminManager adminManager = null)
        {
            (AdminClient, _adminClientScope) = WebUtils.GetScopedServiceOrNull<IAdminClient>();
            if (adminManager is null)
                (AdminManager, _adminManagerScope) = WebUtils.GetScopedServiceOrNull<IAdminManager>();

            AdminManager = adminManager;

            RuleFor(m => m.Name)
                .RequiredWithMessage()
                .MinLengthWithMessage(3)
                .MaxLengthWithMessage(25)
                .AlphaNumericWithMessage()
                .RoleNotInUseWithMessage(AdminClient, AdminManager);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            _adminClientScope?.Dispose();
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