using System;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils;
using CommonLib.Web.Source.Services.Account.Interfaces;
using CommonLib.Web.Source.Validators.Upload;
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
                .SetValidator(new FileSavedToUserFolderValidator());
                //.FileSizeWithMessage(fs => fs <= new FileSize(50, FileSizeSuffix.MB))
                //.FileExtensionWithMessage(".png", ".jpg", ".bmp", ".gif", ".mkv")
                //.FilesUploadedWithMessage();

            RuleFor(m => m.Salary)
                .BetweenWithMessage(1000, 3000);
            
            RuleFor(m => m.DateOfBirth)
                .BetweenWithMessage(new DateTime(1890, 5, 5), new DateTime(1995, 8, 31));

            RuleFor(m => m.AvailableFrom)
                .BetweenWithMessage(new DateTime(2022, 12, 30, 12, 30, 0), new DateTime(2023, 2, 20, 17, 26, 32));
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
