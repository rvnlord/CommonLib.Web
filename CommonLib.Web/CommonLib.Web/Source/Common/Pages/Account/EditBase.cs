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
using CommonLib.Web.Source.Common.Components.MyNavBarComponent;
using CommonLib.Web.Source.Common.Components.MyPasswordInputComponent;
using CommonLib.Web.Source.Common.Components.MyPromptComponent;
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
        
        protected override async Task OnAfterFirstRenderAsync()
        {
            if (!await EnsureAuthenticatedAsync(true, true))
                return;
            
            _allControls = GetInputControls();
            _btnSave = _allControls.OfType<MyButtonBase>().Single(b => b.SubmitsForm.V == true);
            _pwdOldPassword = _allControls.OfType<MyPasswordInputBase>().Single(p => p.For.GetPropertyName().EqualsInvariant(nameof(_editUserVM.OldPassword)));

            Mapper.Map(AuthenticatedUser, _editUserVM);
            _editUserVM.Avatar = (await AccountClient.GetUserAvatarByNameAsync(_editUserVM.UserName)).Result;
            if (!_editUserVM.HasPassword)
                _pwdOldPassword.InteractionState.ParameterValue = ComponentState.ForceDisabled;

            await SetControlStatesAsync(ComponentState.Enabled, _allControls);
            //await StateHasChangedAsync(true);
        }

        protected async Task BtnSubmit_ClickAsync()
        {
            await SetControlStatesAsync(ComponentState.Disabled, _allControls, _btnSave);

            if (!await EnsureAuthenticatedAsync(true, false))
            {
                await SetControlStatesAsync(ComponentState.Disabled, _allControls);
                await ShowLoginModalAsync();
                return;
            }

            if (!await _editContext.ValidateAsync())
                return;

            await WaitForControlsToRerenderAsync(_allControls);

            var editResponse = await AccountClient.EditAsync(_editUserVM);
            if (editResponse.IsError)
            {
                await _validator.AddValidationMessages(editResponse.ValidationMessages).NotifyValidationStateChangedAsync(_validator);
                await PromptMessageAsync(NotificationType.Error, editResponse.Message);
                await SetControlStatesAsync(ComponentState.Enabled, _allControls);
                return;
            }

            Mapper.Map(editResponse.Result, _editUserVM);
            await PromptMessageAsync(NotificationType.Success, editResponse.Message);

            AuthenticatedUser.Avatar = (await AccountClient.GetUserAvatarByNameAsync(AuthenticatedUser.UserName))?.Result;
            await EnsureAuthenticationPerformedAsync(false, true);
            if (HasAuthenticationStatus(AuthStatus.Authenticated))
            {
                if (_editUserVM.HasPassword && _pwdOldPassword.InteractionState.V == ComponentState.ForceDisabled)
                    _pwdOldPassword.InteractionState.ParameterValue = ComponentState.Enabled;
                await SetControlStatesAsync(ComponentState.Enabled, _allControls);
            }
            else
            {
                await SetControlStatesAsync(ComponentState.Disabled, _allControls);
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
