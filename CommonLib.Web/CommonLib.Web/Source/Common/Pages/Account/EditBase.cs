using System.Linq;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyEditFormComponent;
using CommonLib.Web.Source.Common.Components.MyFluentValidatorComponent;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Components.MyPasswordInputComponent;
using CommonLib.Web.Source.Common.Components.MyPromptComponent;
using CommonLib.Web.Source.Common.Components.MyTextInputComponent;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Truncon.Collections;

namespace CommonLib.Web.Source.Common.Pages.Account
{
    [Authorize]
    public class EditBase : MyComponentBase
    {
        private MyComponentBase[] _allControls;
        private MyButtonBase _btnSave;
        private MyPasswordInputBase _pwdOldPassword;

        protected MyFluentValidator _validator { get; set; }
        protected MyEditForm _editForm { get; set; }
        protected MyEditContext _editContext { get; set; }
        protected EditUserVM _editUserVM { get; set; }

        protected override async Task OnInitializedAsync()
        {
            _editUserVM = new(); 
            _editContext = new MyEditContext(_editUserVM);
            await Task.CompletedTask;
        }

        protected override async Task OnParametersSetAsync() 
        {
            await Task.CompletedTask;
        }
        
        protected override async Task OnAfterFirstRenderAsync()
        {
            if (!await EnsureAuthenticatedAsync(true))
                return;
            
            _allControls = Descendants.Where(c => c is MyTextInput or MyPasswordInput or MyButton && !c.Ancestors.Any(a => a is MyInputBase)).ToArray();
            _btnSave = Descendants.OfType<MyButtonBase>().Single(b => b.SubmitsForm.V == true);
            _pwdOldPassword = _allControls.OfType<MyPasswordInputBase>().Single(p => p.For.GetPropertyName().EqualsInvariant(nameof(_editUserVM.OldPassword)));

            Mapper.Map(AuthenticatedUser, _editUserVM);
            if (!_editUserVM.HasPassword)
                _pwdOldPassword.State.ParameterValue = InputState.ForceDisabled;

            await SetControlStatesAsync(ButtonState.Enabled, _allControls);
        }
        
        protected async Task BtnSubmit_ClickAsync()
        {
            await SetControlStatesAsync(ButtonState.Disabled, _allControls, _btnSave);

            if (!await EnsureAuthenticatedAsync(true))
            {
                await SetControlStatesAsync(ButtonState.Disabled, _allControls);
                await ShowLoginModalAsync();
                return;
            }

            if (!await _editContext.ValidateAsync())
                return;
            
            await WaitForControlsToRerenderAsync(_allControls);

            var editResponse = await AccountClient.EditAsync(_editUserVM);
            if (editResponse.IsError)
            {
                _validator.AddValidationMessages(editResponse.ValidationMessages).NotifyValidationStateChanged(_validator);
                await PromptMessageAsync(NotificationType.Error, editResponse.Message);
                await SetControlStatesAsync(ButtonState.Enabled, _allControls);
                return;
            }

            Mapper.Map(editResponse.Result, _editUserVM);
            await PromptMessageAsync(NotificationType.Success, editResponse.Message);

            await EnsureAuthenticationPerformedAsync(false);
            if (HasAuthenticationStatus(AuthStatus.Authenticated))
            {
                if (_editUserVM.HasPassword && _pwdOldPassword.State.V == InputState.ForceDisabled)
                    _pwdOldPassword.State.ParameterValue = InputState.Enabled;
                await SetControlStatesAsync(ButtonState.Enabled, _allControls);
            }
            else
            {
                await SetControlStatesAsync(ButtonState.Disabled, _allControls);
                NavigationManager.NavigateTo($"/Account/ConfirmEmail/?{GetNavQueryStrings()}"); // TODO: test it
            }
        }

        private string GetNavQueryStrings()
        {
            return new OrderedDictionary<string, string>
            {
                [nameof(_editUserVM.Email).PascalCaseToCamelCase()] = _editUserVM.Email?.UTF8ToBase58(false),
            }.Where(kvp => kvp.Value != null).ToQueryString();
        }
    }
}
