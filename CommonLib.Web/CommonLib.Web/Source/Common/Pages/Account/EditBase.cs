using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyEditFormComponent;
using CommonLib.Web.Source.Common.Components.MyFluentValidatorComponent;
using CommonLib.Web.Source.Common.Components.MyPasswordInputComponent;
using CommonLib.Web.Source.Common.Components.MyPromptComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Nethereum.Contracts.QueryHandlers.MultiCall;
using Nethereum.UI;
using Nethereum.Web3;
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
            var queryUser = NavigationManager.GetQueryString<string>("user")?.Base58ToUTF8OrNull()?.JsonDeserializeOrNull()?.To<LoginUserVM>();
            if (!await EnsureAuthenticatedAsync(queryUser is null || queryUser.Mode == ExternalLoginUsageMode.Connection, true)) // I don't want message before potentially logging in with query user
                return;
            
            Mapper.Map(AuthenticatedUser, _editUserVM);
            _editUserVM.ExternalLogins = (await AccountClient.GetExternalLogins(_editUserVM.UserName)).Result;
            _editUserVM.Wallets = (await AccountClient.GetWalletsAsync(_editUserVM.UserName)).Result;
            _editUserVM.Avatar = (await AccountClient.GetUserAvatarByNameAsync(_editUserVM.UserName)).Result;

            await _editForm.StateHasChangedAsync(true, true);

            _allControls = GetInputControls();
            _btnSave = _allControls.OfType<MyButtonBase>().Single(b => b.SubmitsForm.V == true);
            _pwdOldPassword = _allControls.OfType<MyPasswordInputBase>().Single(p => p.For.GetPropertyName().EqualsInvariant(nameof(_editUserVM.OldPassword)));
            if (!_editUserVM.HasPassword)
                _pwdOldPassword.InteractivityState.StateValue = ComponentState.ForceDisabled;
            
            if (queryUser is not null && queryUser.Mode == ExternalLoginUsageMode.Connection)
            {
                queryUser.UserName = _editUserVM.UserName;
                await ConnectExternalLoginAsync(queryUser);
                return;
            }

            await SetControlStatesAsync(ComponentState.Enabled, _allControls);
            var loginControls = (await ComponentByTypeAsync<LoginBase>()).GetInputControls(); // re-enable controls disabled when clicking `edit` nav link (button)
            if (loginControls.All(c => c.InteractivityState.V.In(ComponentState.Disabled, ComponentState.Loading)) && loginControls.Count(c => c.InteractivityState.V == ComponentState.Loading) == 1)
                await SetControlStatesAsync(ComponentState.Enabled, loginControls);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender, bool authUserChanged)
        {
            if (firstRender) 
                return;
            if (!authUserChanged)
                return;

            if (HasAuthenticationStatus(AuthStatus.Authenticated))
                await OnAfterFirstRenderAsync();
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

            //await WaitForControlsToRerenderAsync(_allControls); // it shouldn't be needed anymore since it is enforced in validation itself

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
                if (_editUserVM.HasPassword && _pwdOldPassword.InteractivityState.V == ComponentState.ForceDisabled)
                    _pwdOldPassword.InteractivityState.StateValue = ComponentState.Enabled;
                await SetControlStatesAsync(ComponentState.Enabled, _allControls);
            }
            else
            {
                await SetControlStatesAsync(ComponentState.Disabled, _allControls);
                NavigationManager.NavigateTo($"/Account/ConfirmEmail/?{GetNavQueryStrings()}"); // TODO: test it
            }
        }

        protected async Task BtnConnectExternalLogin_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken token)
        {
            await SetControlStatesAsync(ComponentState.Disabled, _allControls, sender);
            
            var url = $"{ConfigUtils.BackendBaseUrl}/api/account/externallogin";
            var user = new LoginUserVM
            {
                ExternalProvider = ((ExternalLoginVM) sender.Model.V).Provider.ToLowerInvariant(),
                ReturnUrl = NavigationManager.Uri.BeforeFirstOrWhole("?"),
                RememberMe = AuthenticatedUser.RememberMe,
                Mode = ExternalLoginUsageMode.Connection
            };
            
            NavigationManager.NavigateTo($"{url}?user={user.JsonSerialize().UTF8ToBase58()}", true);
        }

        private async Task ConnectExternalLoginAsync(LoginUserVM queryUser)
        {
            var btnCurrentExternalLoginConnect = (await ComponentsByTypeAsync<MyButtonBase>()).Single(b => (b.Model?.V as ExternalLoginVM)?.Provider.EqualsIgnoreCase_(queryUser.ExternalProvider) == true);
            await SetControlStatesAsync(ComponentState.Disabled, _allControls, btnCurrentExternalLoginConnect);
            var connectResp = await AccountClient.ConnectExternalLoginAsync(_editUserVM, queryUser);
            if (connectResp.IsError)
            {
                await PromptMessageAsync(NotificationType.Error, connectResp.Message);
                await SetControlStatesAsync(ComponentState.Enabled, _allControls);
                return;
            }
            
            Mapper.Map(connectResp.Result, _editUserVM);

            await PromptMessageAsync(NotificationType.Success, connectResp.Message);
            await StateHasChangedAsync(true); // to re-loop providers
            _allControls = GetInputControls(); // to update providers controls to remove the disconnected one
            await SetControlStatesAsync(ComponentState.Enabled, _allControls);
        }

        protected async Task BtnDisconnectExternalLogin_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken token)
        {
            await SetControlStatesAsync(ComponentState.Disabled, _allControls, sender);
            _editUserVM.ExternalProviderToDisconnect = ((ExternalLoginVM) sender.Model.V).Provider.ToLowerInvariant();
            var disconnectResp = await AccountClient.DisconnectExternalLoginAsync(_editUserVM);
            if (disconnectResp.IsError)
            {
                await PromptMessageAsync(NotificationType.Error, disconnectResp.Message);
                await SetControlStatesAsync(ComponentState.Enabled, _allControls);
                return;
            }

            Mapper.Map(disconnectResp.Result, _editUserVM);

            await PromptMessageAsync(NotificationType.Success, disconnectResp.Message);
            await StateHasChangedAsync(true);
            _allControls = GetInputControls();
            await SetControlStatesAsync(ComponentState.Enabled, _allControls);
        }
        
        private string GetNavQueryStrings()
        {
            return new OrderedDictionary<string, string>
            {
                [nameof(_editUserVM.Email).PascalCaseToCamelCase()] = _editUserVM.Email?.UTF8ToBase58(false),
            }.Where(kvp => kvp.Value is not null).ToQueryString();
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _btnSave?.Dispose();
                _pwdOldPassword?.Dispose();
            }
            
            base.Dispose(disposing);
        }
    }
}
