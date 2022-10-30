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
using CommonLib.Web.Source.Common.Components.MyModalComponent;
using CommonLib.Web.Source.Common.Components.MyNavLinkComponent;
using CommonLib.Web.Source.Common.Components.MyPasswordInputComponent;
using CommonLib.Web.Source.Common.Components.MyPromptComponent;
using CommonLib.Web.Source.Common.Components.MyTextInputComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Models;
using CommonLib.Web.Source.ViewModels.Account;
using Microsoft.AspNetCore.Components.Forms;
using Truncon.Collections;

namespace CommonLib.Web.Source.Common.Pages.Account
{
    public class ResetPasswordBase : MyComponentBase
    {
        private MyComponentBase[] _allControls;
        private MyButtonBase _btnResetPassword;

        protected MyFluentValidator _validator { get; set; }
        protected BlazorParameter<ButtonState?> _bpBtnResetPasswordState { get; set; }
        protected MyEditForm _editForm { get; set; }
        protected MyEditContext _editContext { get; set; }
        protected ResetPasswordUserVM _resetPasswordUserVM { get; set; }
        
        protected override async Task OnInitializedAsync()
        {
            _resetPasswordUserVM = new()
            {
                Email = NavigationManager.GetQueryString<string>("email")?.Base58ToUTF8(),
                ResetPasswordCode = NavigationManager.GetQueryString<string>("code"),
                ReturnUrl = NavigationManager.GetQueryString<string>("returnUrl")?.Base58ToUTF8() ?? "/Account/Login"
            };
            _editContext = new MyEditContext(_resetPasswordUserVM);
            await Task.CompletedTask;
        }

        protected override async Task OnParametersSetAsync()
        {
            if (FirstParamSetup)
            {
                _bpBtnResetPasswordState = ButtonState.Loading;
                await SetUserNameAsync();
                _editContext.ReBindValidationStateChanged(CurrentEditContext_ValidationStateChangedAsync);
            }
        }

        protected override async Task OnAfterFirstRenderAsync()
        {
            if (!_resetPasswordUserVM.Email.IsNullOrWhiteSpace() && !_resetPasswordUserVM.ResetPasswordCode.IsNullOrWhiteSpace())
                await _editContext.ValidateFieldAsync(() => _resetPasswordUserVM.ResetPasswordCode);
            
            _allControls = Descendants.Where(c => c is MyTextInput or MyPasswordInput or MyButton or MyNavLink && !c.Ancestors.Any(a => a is MyInputBase)).ToArray();
            _btnResetPassword = Descendants.OfType<MyButtonBase>().Single(b => b.SubmitsForm.V == true);

            await SetControlStatesAsync(ButtonState.Enabled, _allControls);
        }
        
        protected async Task BtnSubmit_ClickAsync() => await _editForm.SubmitAsync();
        protected async Task FormResetPassword_ValidSubmitAsync() => await ResetPasswordAsync();

        private async Task ResetPasswordAsync()
        {
            await SetControlStatesAsync(ButtonState.Disabled, _allControls, _btnResetPassword);
            var resetPasswordResponse = await AccountClient.ResetPasswordAsync(_resetPasswordUserVM);
            if (resetPasswordResponse.IsError)
            {
                await _validator.AddValidationMessages(resetPasswordResponse.ValidationMessages).NotifyValidationStateChangedAsync(_validator);
                await PromptMessageAsync(NotificationType.Error, resetPasswordResponse.Message);
                await SetControlStatesAsync(ButtonState.Enabled, _allControls);
                return;
            }

            Mapper.Map(resetPasswordResponse.Result, _resetPasswordUserVM);
            await PromptMessageAsync(NotificationType.Success, resetPasswordResponse.Message);
            await ComponentByClassAsync<MyModalBase>("my-login-modal").ShowModalAsync();
            await SetControlStatesAsync(ButtonState.Disabled, _allControls);
        }

        private async Task CurrentEditContext_ValidationStateChangedAsync(MyEditContext sender, MyValidationStateChangedEventArgs e, CancellationToken _)
        {
            if (e == null)
                throw new NullReferenceException(nameof(e));
            if (e.ValidationMode == ValidationMode.Property && e.ValidatedFields == null)
                throw new NullReferenceException(nameof(e.ValidatedFields));
            if (e.ValidationStatus == ValidationStatus.Pending)
                return;

            if (new FieldIdentifier(_resetPasswordUserVM, nameof(_resetPasswordUserVM.Email)).In(e.ValidFields))
                await SetUserNameAsync();
            else
                _resetPasswordUserVM.UserName = null;

            await StateHasChangedAsync();
        }

        private async Task SetUserNameAsync() => _resetPasswordUserVM.UserName = (await AccountClient.FindUserByEmailAsync(_resetPasswordUserVM.Email)).Result?.UserName;

        protected string GetNavQueryStrings()
        {
            return new OrderedDictionary<string, string>
            {
                [nameof(_resetPasswordUserVM.Email).PascalCaseToCamelCase()] = _resetPasswordUserVM.Email?.UTF8ToBase58(false),
                [nameof(_resetPasswordUserVM.ReturnUrl).PascalCaseToCamelCase()] = _resetPasswordUserVM.ReturnUrl?.UTF8ToBase58()
            }.Where(kvp => kvp.Value != null).ToQueryString();
        }
    }
}
