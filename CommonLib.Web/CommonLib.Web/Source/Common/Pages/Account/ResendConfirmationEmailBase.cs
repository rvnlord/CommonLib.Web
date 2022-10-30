using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Components;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyEditFormComponent;
using CommonLib.Web.Source.Common.Components.MyFluentValidatorComponent;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Components.MyNavLinkComponent;
using CommonLib.Web.Source.Common.Components.MyPasswordInputComponent;
using CommonLib.Web.Source.Common.Components.MyPromptComponent;
using CommonLib.Web.Source.Common.Components.MyTextInputComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.ViewModels.Account;
using Microsoft.AspNetCore.Components.Forms;
using Truncon.Collections;

namespace CommonLib.Web.Source.Common.Pages.Account
{
    public class ResendConfirmationEmailBase : MyComponentBase
    {
        private MyComponentBase[] _allControls;
        private MyButtonBase _btnResendConfirmationEmail;

        protected MyFluentValidator _validator;
        protected MyEditForm _editForm;
        protected MyEditContext _editContext;
        protected ResendConfirmationEmailUserVM _resendConfirmationEmailUserVM;
        
        protected override async Task OnInitializedAsync()
        {
            _resendConfirmationEmailUserVM = new()
            {
                Email = NavigationManager.GetQueryString<string>("email"),
                ReturnUrl = NavigationManager.GetQueryString<string>("returnUrl")?.Base58ToUTF8() ?? "/Account/Login" // we can arrive here from register, login or if user types this address but we want to redirect to confirm email from here regardless
            };
            _editContext = new MyEditContext(_resendConfirmationEmailUserVM);
            await Task.CompletedTask;
        }

        protected override async Task OnParametersSetAsync()
        {
            if (IsFirstParamSetup())
            {
                await SetUserNameAsync();
                _editContext.ReBindValidationStateChanged(CurrentEditContext_ValidationStateChangedAsync);
            }
            await Task.CompletedTask;
        }

        protected override async Task OnAfterFirstRenderAsync()
        {
            _allControls = Descendants.Where(c => c is MyTextInput or MyPasswordInput or MyButton or MyNavLink && !c.Ancestors.Any(a => a is MyInputBase)).ToArray();
            _btnResendConfirmationEmail = Descendants.OfType<MyButtonBase>().Single(b => b.SubmitsForm.V == true);

            await SetControlStatesAsync(ButtonState.Enabled, _allControls);
        }

        protected async Task BtnSubmit_ClickAsync() => await _editForm.SubmitAsync();

        public async Task FormResendConfirmationEmail_ValidSubmitAsync()
        {
            await SetControlStatesAsync(ButtonState.Disabled, _allControls, _btnResendConfirmationEmail);
            var resendEmailConfirmationResponse = await AccountClient.ResendConfirmationEmailAsync(_resendConfirmationEmailUserVM);
            if (resendEmailConfirmationResponse.IsError)
            {
                await _validator.AddValidationMessages(resendEmailConfirmationResponse.ValidationMessages).NotifyValidationStateChangedAsync(_validator);
                await PromptMessageAsync(NotificationType.Error, resendEmailConfirmationResponse.Message);
                await SetControlStatesAsync(ButtonState.Enabled, _allControls);
                return;
            }

            await SetControlStatesAsync(ButtonState.Disabled, _allControls);
            await PromptMessageAsync(NotificationType.Success, resendEmailConfirmationResponse.Message);
            NavigationManager.NavigateTo($"/Account/ConfirmEmail/?{GetNavQueryStrings()}");
        }

        private async Task CurrentEditContext_ValidationStateChangedAsync(MyEditContext sender, MyValidationStateChangedEventArgs e, CancellationToken _)
        {
            if (e == null)
                throw new NullReferenceException(nameof(e));
            if (e.ValidationMode == ValidationMode.Property && e.ValidatedFields == null)
                throw new NullReferenceException(nameof(e.ValidatedFields));
            if (e.ValidationStatus == ValidationStatus.Pending)
                return;

            if (new FieldIdentifier(_resendConfirmationEmailUserVM, nameof(_resendConfirmationEmailUserVM.Email)).In(e.ValidatedFields)) // as long as user tried to validate email
                await SetUserNameAsync();

            await StateHasChangedAsync();
        }

        private async Task SetUserNameAsync() => _resendConfirmationEmailUserVM.UserName = (await AccountClient.FindUserByEmailAsync(_resendConfirmationEmailUserVM.Email))?.Result?.UserName;

        protected string GetNavQueryStrings()
        {
            return new OrderedDictionary<string, string>
            {
                [nameof(_resendConfirmationEmailUserVM.Email).PascalCaseToCamelCase()] = _resendConfirmationEmailUserVM.Email?.UTF8ToBase58(false),
                [nameof(_resendConfirmationEmailUserVM.ReturnUrl).PascalCaseToCamelCase()] = _resendConfirmationEmailUserVM.ReturnUrl?.UTF8ToBase58()
            }.Where(kvp => kvp.Value != null).ToQueryString();
        }
    }
}
