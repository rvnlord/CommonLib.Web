using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils.TypeUtils;
using CommonLib.Web.Source.Common.Components;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyEditContextComponent;
using CommonLib.Web.Source.Common.Components.MyEditFormComponent;
using CommonLib.Web.Source.Common.Components.MyFluentValidatorComponent;
using CommonLib.Web.Source.Common.Components.MyPasswordInputComponent;
using CommonLib.Web.Source.Common.Components.MyPromptComponent;
using CommonLib.Web.Source.Common.Components.MyTextInputComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Truncon.Collections;

namespace CommonLib.Web.Source.Common.Pages.Account
{
    [Authorize]
    public class EditBase : MyComponentBase
    {
        private OrderedDictionary<MyComponentBase, bool> _controlsRenderingStatus;
        private MyComponentBase[] _allControls;
        private MyComponentBase[] _disabledComponents;

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

        protected override async Task OnInitializedAsync()
        {
            _editUserVM = new(); 
            _editContext = new MyEditContext(_editUserVM);
            _controlsRenderingStatus = new OrderedDictionary<MyComponentBase, bool>();
            await Task.CompletedTask;
        }

        protected override async Task OnParametersSetAsync() 
        {
            await Task.CompletedTask;
        }
        
        protected override async Task OnAfterFirstRenderAsync()
        {
            if (!await EnsureAuthenticatedAsync())
                return;
            
            var layout = Layout;
            var parent = Parent;
            var children = Children;
            var descendants = Descendants;
            
            _allControls = this.GetPropertyNames().Select(this.GetPropertyOrNull<MyComponentBase>).Where(c => c is not null)
                .Where(c => c.GetPropertyOrNull("State") is not null).ToArray();
            foreach (var control in _allControls)
            {
                control.AfterRenderFinished -= Control_AfterRenderFinished;
                control.AfterRenderFinished += Control_AfterRenderFinished;
            }

            Mapper.Map(AuthenticatedUser, _editUserVM);
            _disabledComponents = _editUserVM.HasPassword ? new MyComponentBase[] { _txtId } : new MyComponentBase[] { _txtId, _pwdOldPassword };
            await SetControlStatesAsync(ButtonState.Enabled, null, _disabledComponents);
        }

        private async Task Control_AfterRenderFinished(MyComponentBase sender, AfterRenderFinishedEventArgs e, CancellationToken token)
        {
            _controlsRenderingStatus[sender] = true;
            await Task.CompletedTask;
        }

        private void ClearControlsRenderingStatus() => _controlsRenderingStatus.Clear();

        private async Task WaitForControlsToRender()
        {
            await TaskUtils.WaitUntil(() => _controlsRenderingStatus.Count == _allControls.Length && _controlsRenderingStatus.Values.All(v => v));
        }

        protected async Task BtnSubmit_ClickAsync()
        {
            ClearControlsRenderingStatus();
            await SetControlStatesAsync(ButtonState.Disabled, _btnSave, _disabledComponents);

            if (!await EnsureAuthenticatedAsync())
            {
                await SetControlStatesAsync(ButtonState.Disabled, null, _disabledComponents);
                await ShowLoginModalAsync();
                return;
            }

            await WaitForControlsToRender();
            ClearControlsRenderingStatus();

            if (!await _editContext.ValidateAsync())
                return;
            
            await WaitForControlsToRender();

            var editResponse = await AccountClient.EditAsync(_editUserVM);
            if (editResponse.IsError)
            {
                _validator.AddValidationMessages(editResponse.ValidationMessages).NotifyValidationStateChanged(_validator);
                await PromptMessageAsync(NotificationType.Error, editResponse.Message);
                await SetControlStatesAsync(ButtonState.Enabled, null, _disabledComponents);
                return;
            }

            Mapper.Map(editResponse.Result, _editUserVM);
            await PromptMessageAsync(NotificationType.Success, editResponse.Message);

            await EnsureAuthenticationPerformedAsync();
            if (HasAuthenticationStatus(AuthStatus.Authenticated))
                await SetControlStatesAsync(ButtonState.Enabled, null, _disabledComponents);
            else
            {
                await SetControlStatesAsync(ButtonState.Disabled, null, _disabledComponents);
                NavigationManager.NavigateTo($"/Account/ConfirmEmail/?{GetNavQueryStrings()}"); // TODO: test it
            }
        }

        private async Task SetControlStatesAsync(ButtonState state, MyButtonBase btnLoading = null, IEnumerable<MyComponentBase> dontChangeComponents = null)
        {
            if (btnLoading != null)
                btnLoading.State.ParameterValue = ButtonState.Loading;

            var controlsToChangeStata = _allControls.AsEnumerable();
            if (btnLoading != null)
                controlsToChangeStata = controlsToChangeStata.Except(btnLoading);
            if (dontChangeComponents != null)
                controlsToChangeStata = controlsToChangeStata.Except(dontChangeComponents);

            var notifyParamsChangedTasks = new List<Task>();
            var changeStateTasks = new List<Task>();
            foreach (var control in controlsToChangeStata.ToArray())
            {
                var stateProp = control.GetProperty("State").GetProperty("ParameterValue");
                var enumType = stateProp.GetType();
                Type propType = null;
                bool? isForcedProp = null;
                var isEnum = enumType.IsEnum;
                if (!isEnum)
                {
                    isForcedProp = stateProp.GetProperty<bool?>("IsForced");
                    stateProp = stateProp.GetProperty("State");
                    propType = enumType;
                    enumType = stateProp.GetType();
                }
                var enumValues = Enum.GetValues(enumType).IColToArray();
                var val = enumValues.Single(v => StringExtensions.EndsWithInvariant(EnumConverter.EnumToString(v.CastToReflected(enumType)), state.EnumToString()));

                if (!isEnum)
                {
                    val = Activator.CreateInstance(propType, val, false);
                    if (isForcedProp != true)
                        control.GetProperty("State").SetProperty("ParameterValue", val);
                }
                else 
                    control.GetProperty("State").SetProperty("ParameterValue", val);

                changeStateTasks.Add((Task<MyComponentBase>) (control.GetType().GetMethod("NotifyParametersChangedAsync")?.Invoke(control, new object[] { true }) ?? throw new NullReferenceException()));
                notifyParamsChangedTasks.Add((Task<MyComponentBase>) (control.GetType().GetMethod("StateHasChangedAsync")?.Invoke(control, new object[] { true }) ?? throw new NullReferenceException()));
            }

            await Task.WhenAll(notifyParamsChangedTasks);
            await Task.WhenAll(changeStateTasks);
            await NotifyParametersChangedAsync().StateHasChangedAsync(true);
        }

        protected string GetNavQueryStrings()
        {
            return new OrderedDictionary<string, string>
            {
                [nameof(_editUserVM.Email).PascalCaseToCamelCase()] = _editUserVM.Email?.UTF8ToBase58(false),
            }.Where(kvp => kvp.Value != null).ToQueryString();
        }
    }
}
