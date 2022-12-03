using System;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils;
using CommonLib.Web.Source.Services.Account.Interfaces;
using FluentValidation;
using SimpleInjector;

namespace CommonLib.Web.Source.Validators.Upload
{
    public class AvatarValidator : AbstractValidator<FileDataList>
    {
        private Scope _accountClientScope;
        private Scope _accountManagerScope;

        public IAccountClient AccountClient { get; set; }
        public IAccountManager AccountManager { get; set; }

        public AvatarValidator()
        {
            Initialize();
        }

        public AvatarValidator(IAccountManager accountManager)
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

            RuleFor(m => m)
                .FileSizeWithMessage(fs => fs <= new FileSize(50, FileSizeSuffix.MB))
                .FileExtensionWithMessage(".png", ".jpg", ".bmp", ".gif")
                .NoFileIsUploadingWithMessage()
                .PotentialAvatarsNotInUseWithMessage(AccountClient, AccountManager);
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

        ~AvatarValidator() {
            Dispose(false);
        }
    }
}