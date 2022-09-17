using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyEditContextComponent;
using CommonLib.Web.Source.Common.Components.MyEditFormComponent;
using CommonLib.Web.Source.Common.Components.MyFluentValidatorComponent;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Components.MyPromptComponent;
using CommonLib.Web.Source.Common.Components.MyTextInputComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Models;
using CommonLib.Web.Source.ViewModels.Account;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Truncon.Collections;

namespace CommonLib.Web.Source.Common.Pages.Account
{
    public class ForgotPasswordBase : MyComponentBase
    {
        protected MyFluentValidator _validator { get; set; }
        protected MyEditForm _editForm { get; set; }
        protected MyEditContext _editContext { get; set; }
        protected MyButtonBase _btnForgotPassword { get; set; }
        protected ForgotPasswordUserVM _forgotPasswordUserVM { get; set; }
        protected MyTextInputBase _txtEmail { get; set; }
        protected BlazorParameter<ButtonState?> _btnForgotPasswordState;
        
        protected override async Task OnInitializedAsync()
        {
            var inheritedReturnUrl = NavigationManager.GetQueryString<string>("returnUrl")?.Base58ToUTF8()?.BeforeFirstOrWhole("?");
            _forgotPasswordUserVM ??= new ForgotPasswordUserVM { ReturnUrl = inheritedReturnUrl ?? NavigationManager.BaseUri };
            _editContext ??= new MyEditContext(_forgotPasswordUserVM);

            await Task.CompletedTask;
        }

        protected override async Task OnParametersSetAsync()
        {
            if (FirstParamSetup)
            {
                _btnForgotPasswordState = ButtonState.Disabled;
                await SetUserNameAsync();
                _editContext.ReBindValidationStateChanged(CurrentEditContext_ValidationStateChangedAsync);
            }
            await Task.CompletedTask;
        }

        protected override async Task OnAfterFirstRenderAsync()
        {
            _btnForgotPassword.State.ParameterValue = ButtonState.Enabled;
            await StateHasChangedAsync(true);
        }

        protected async Task BtnChangeForgottenPassword_ClickAsync() => await _editForm.SubmitAsync();
        protected async Task FormChangeForgottenPassword_ValidSubmitAsync()
        {
            await SetControlStatesAsync(ButtonState.Disabled, _btnForgotPassword);
            var forgotPasswordResponse = await AccountClient.ForgotPasswordAsync(_forgotPasswordUserVM);

            if (forgotPasswordResponse.IsError)
            {
                _btnForgotPassword.State.ParameterValue = ButtonState.Enabled;
                _validator.AddValidationMessages(forgotPasswordResponse.ValidationMessages).NotifyValidationStateChanged(_validator); ;
                await PromptMessageAsync(NotificationType.Error, forgotPasswordResponse.Message);
                await SetControlStatesAsync(ButtonState.Enabled);
                return;
            }

            Mapper.Map(forgotPasswordResponse.Result, _forgotPasswordUserVM);
            await PromptMessageAsync(NotificationType.Success, forgotPasswordResponse.Message);
            NavigationManager.NavigateTo($"/Account/ResetPassword?{GetNavQueryStrings()}");
        }

        private async Task CurrentEditContext_ValidationStateChangedAsync(object sender, MyValidationStateChangedEventArgs e)
        {
            if (e == null)
                throw new NullReferenceException(nameof(e));
            if (e.ValidationMode == ValidationMode.Property && e.ValidatedFields == null)
                throw new NullReferenceException(nameof(e.ValidatedFields));
            if (e.ValidationStatus == ValidationStatus.Pending)
                return;

            if (new FieldIdentifier(_forgotPasswordUserVM, nameof(_forgotPasswordUserVM.Email)).In(e.ValidatedFields))
                await SetUserNameAsync();

            await StateHasChangedAsync();
        }

        private async Task SetUserNameAsync() => _forgotPasswordUserVM.UserName = (await AccountClient.FindUserByEmailAsync(_forgotPasswordUserVM.Email))?.Result?.UserName;
        
        protected string GetNavQueryStrings()
        {
            return new OrderedDictionary<string, string>
            {
                [nameof(_forgotPasswordUserVM.Email).PascalCaseToCamelCase()] = _forgotPasswordUserVM.Email?.UTF8ToBase58(false),
                [nameof(_forgotPasswordUserVM.ReturnUrl).PascalCaseToCamelCase()] = _forgotPasswordUserVM.ReturnUrl?.UTF8ToBase58()
            }.Where(kvp => kvp.Value != null).ToQueryString();
        }

        private async Task SetControlStatesAsync(ButtonState otherControlsState, MyButtonBase btnLoading = null)
        {
            if (btnLoading != null)
                btnLoading.State.ParameterValue = ButtonState.Loading;

            var otherButtons = new[] { _btnForgotPassword }.Except(btnLoading).ToArray();
            foreach (var btn in otherButtons)
                if (btn != null)
                    btn.State.ParameterValue = otherControlsState;
            
            MyInputBase[] allInputs = { _txtEmail };
            foreach (var input in allInputs)
                input.State.ParameterValue = otherControlsState == ButtonState.Enabled && !input.State.ParameterValue.IsForced ? InputState.Enabled : InputState.Disabled;
            
            await StateHasChangedAsync(true);
        }
    }
}
