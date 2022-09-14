using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils.TypeUtils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyEditContextComponent;
using CommonLib.Web.Source.Common.Components.MyEditFormComponent;
using CommonLib.Web.Source.Common.Components.MyFluentValidatorComponent;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Components.MyPasswordInputComponent;
using CommonLib.Web.Source.Common.Components.MyPromptComponent;
using CommonLib.Web.Source.Common.Components.MyTextInputComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Models;
using CommonLib.Web.Source.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace CommonLib.Web.Source.Common.Pages.Account
{
    [Authorize]
    public class EditBase : MyComponentBase
    {
        protected MyFluentValidator _validator { get; set; }
        protected MyEditForm _editForm { get; set; }
        protected MyEditContext _editContext { get; set; }
        protected EditUserVM _editUserVM { get; set; }
        protected MyButtonBase _btnSave { get; set; }
        protected MyTextInputBase _txtId { get; set; }
        protected MyTextInputBase _txtUserName { get; set; }
        protected MyTextInputBase _txtEmail { get; set; }
        protected MyPasswordInputBase _pwdOldPassword { get; set; }
        protected MyPasswordInputBase _pwdNewPassword { get; set; }
        protected MyPasswordInputBase _pwdConfirmNewPassword { get; set; }
        protected BlazorParameter<ButtonState?> _bpBtnSaveState { get; set; }
        protected BlazorParameter<InputState?> _bpTxtIdState { get; set; }
        protected BlazorParameter<InputState?> _bpTxtUserNameState { get; set; }
        protected BlazorParameter<InputState?> _bpTxtEmailState { get; set; }
        protected BlazorParameter<InputState?> _bpPwdOldPasswordState { get; set; }
        protected BlazorParameter<InputState?> _bpPwdNewPasswordState { get; set; }
        protected BlazorParameter<InputState?> _bpPwdConfirmNewPasswordState { get; set; }

        [Inject]
        public IMapper Mapper { get; set; }
        
        protected override async Task OnInitializedAsync()
        {
            _editUserVM = new(); 
            _editContext = new MyEditContext(_editUserVM);
            await Task.CompletedTask;
        }

        protected override async Task OnParametersSetAsync()
        {
            if (FirstParamSetup)
            {
                _bpBtnSaveState = ButtonState.Disabled;
                _bpTxtIdState = InputState.Disabled;
                _bpTxtUserNameState = InputState.Disabled;
                _bpTxtEmailState = InputState.Disabled;
                _bpPwdOldPasswordState = InputState.Disabled;
                _bpPwdNewPasswordState = InputState.Disabled;
                _bpPwdConfirmNewPasswordState = InputState.Disabled;
            }

            // TODO: find a way to disable parts of inputs (icons, buttons) before render

            await Task.CompletedTask;
        }
        
        protected override async Task OnAfterFirstRenderAsync()
        {
            var authResponse = await AccountClient.GetAuthenticatedUserAsync();
            if (authResponse.IsError || !authResponse.Result.IsAuthenticated)
            {
                await PromptMessageAsync(NotificationType.Error, authResponse.Message);
                UserAuthStateProvider.StateChanged();
                return;
            }

            AuthenticatedUser = authResponse.Result;
            Mapper.Map(AuthenticatedUser, _editUserVM);
            var disabledComponents = _editUserVM.HasPassword ? new[] { _txtId } : new MyComponentBase[] { _txtId, _pwdOldPassword };
            await SetControlStatesAsync(ButtonState.Enabled, null, disabledComponents);
        }
        
        protected async Task BtnSubmit_ClickAsync() => await _editForm.SubmitAsync();
        protected async Task FormEdit_ValidSubmitAsync()
        {
            await SetControlStatesAsync(ButtonState.Disabled, _btnSave);

            if (!IsAuthenticated())
            {
                await PromptMessageAsync(NotificationType.Error, "You are not Authenticated");
                await SetControlStatesAsync(ButtonState.Disabled);
                await ShowLoginModalAsync();
                return;
            }
            
            var editResponse = await AccountClient.EditAsync(_editUserVM);
            if (editResponse.IsError)
            {
                _validator.AddValidationMessages(editResponse.ValidationMessages).NotifyValidationStateChanged(_validator);
                await PromptMessageAsync(NotificationType.Error, editResponse.Message);
                await SetControlStatesAsync(ButtonState.Enabled);
                return;
            }

            Mapper.Map(editResponse.Result, _editUserVM);
            await PromptMessageAsync(NotificationType.Success, editResponse.Message);
            UserAuthStateProvider.StateChanged(); // mandatory since we are logging user out if the email was changed
        }

        private async Task SetControlStatesAsync(ButtonState state, MyButtonBase btnLoading = null, IEnumerable<MyComponentBase> dontChangeComponents = null)
        {
            if (btnLoading != null)
                btnLoading.State.ParameterValue = ButtonState.Loading;
            
            var allControls = this.GetPropertyNames().Select(this.GetPropertyOrNull<MyComponentBase>).Where(c => c is not null).ToArray();
            var controlsWithState = allControls.Where(c => c.GetPropertyOrNull("State")?.GetPropertyOrNull("ParameterValue").GetType().IsEnum == true);
            if (btnLoading != null)
                controlsWithState = controlsWithState.Except(btnLoading);
            if (dontChangeComponents != null)
                controlsWithState = controlsWithState.Except(dontChangeComponents);

            foreach (var control in controlsWithState)
            {
                var propType = control.GetProperty("State").GetProperty("ParameterValue").GetType();
                var enumValues = Enum.GetValues(propType).IColToArray();
                var val = enumValues.Single(v => StringExtensions.EndsWithInvariant(EnumConverter.EnumToString(v.CastToReflected(propType)), state.EnumToString()));
                control.GetProperty("State").SetPropertyValue("ParameterValue", val);
                await (Task<MyComponentBase>) (control.GetType().GetMethod("NotifyParametersChangedAsync")?.Invoke(control, Array.Empty<object>()) ?? throw new NullReferenceException());
                await (Task<MyComponentBase>) (control.GetType().GetMethod("StateHasChangedAsync")?.Invoke(control, new object[] { true }) ?? throw new NullReferenceException());
            }
            
            await NotifyParametersChangedAsync().StateHasChangedAsync(true);
        }
    }
}
