using System;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils;
using CommonLib.Web.Source.Services.Account.Interfaces;
using CommonLib.Web.Source.ViewModels;
using FluentValidation;
using SimpleInjector;

namespace CommonLib.Web.Source.Validators
{
    public class TestEmployeeVMValidator : AbstractValidator<TestEmployeeVM>, IDisposable
    {
        private readonly Scope _accountClientScope;
        private readonly Scope _accountManagerScope;

        public TestEmployeeVMValidator()
        {
            (var accountClient, _accountClientScope) = WebUtils.GetScopedServiceOrNull<IAccountClient>();
            (var accountManager, _accountManagerScope) = WebUtils.GetScopedServiceOrNull<IAccountManager>();

            ClassLevelCascadeMode = CascadeMode.Continue;
            RuleLevelCascadeMode = CascadeMode.Stop;
            
            RuleFor(m => m.Name)
                .RequiredWithMessage()
                .MinLengthWithMessage(3)
                .MaxLengthWithMessage(25)
                .UserManagerCompliantWithMessage(accountClient, accountManager);

            RuleFor(m => m.Email)
                .RequiredWithMessage()
                .EmailAddressWithMessage();

            RuleFor(m => m.Department)
                .RequiredWithMessage();

            RuleFor(m => m.Domain)
                .RequiredWithMessage();

            RuleFor(m => m.Password)
                .RequiredWithMessage()
                .UserManagerCompliantWithMessage(accountClient, accountManager);

            RuleFor(m => m.Gender)
                .RequiredWithMessage()
                .NotEqualWithMessage(Gender.Male);

            RuleFor(m => m.TermsAccepted)
                .RequiredWithMessage()
                .EqualWithMessage(true);

            RuleFor(m => m.Files)
                .FileSizeWithMessage(fs => fs <= new FileSize(50, FileSizeSuffix.MB))
                .FileExtensionWithMessage(".png", ".jpg", ".bmp", ".gif", ".mkv");
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

        ~TestEmployeeVMValidator() {
            Dispose(false);
        }
    }

}
