using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyCssGridItemComponent;
using CommonLib.Web.Source.Common.Components.MyEditFormComponent;
using CommonLib.Web.Source.Common.Components.MyFluentValidatorComponent;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Components.MyNavBarComponent;
using CommonLib.Web.Source.Common.Components.MyPasswordInputComponent;
using CommonLib.Web.Source.Common.Components.MyPromptComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Nethereum.Siwe.Core.Recap;
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
        //protected LoginUserVM _loginUserVM { get; set; }

        protected override async Task OnInitializedAsync()
        {
            _editUserVM = new(); 
            _editContext = new MyEditContext(_editUserVM);

            //_loginUserVM ??= new LoginUserVM
            //{
            //    ReturnUrl = NavigationManager.Uri.BeforeFirstOrWhole("?")
            //};

            await Task.CompletedTask;
        }
        
        protected override async Task OnAfterFirstRenderAsync()
        {
            if (!await EnsureAuthenticatedAsync(true, true))
                return;
            
            Mapper.Map(AuthenticatedUser, _editUserVM);
            _editUserVM.ExternalLogins = (await AccountClient.GetExternalLogins(_editUserVM.UserName)).Result;
            _editUserVM.Avatar = (await AccountClient.GetUserAvatarByNameAsync(_editUserVM.UserName)).Result;

            await _editForm.StateHasChangedAsync(true, true);

            _allControls = GetInputControls();
            _btnSave = _allControls.OfType<MyButtonBase>().Single(b => b.SubmitsForm.V == true);
            _pwdOldPassword = _allControls.OfType<MyPasswordInputBase>().Single(p => p.For.GetPropertyName().EqualsInvariant(nameof(_editUserVM.OldPassword)));
            if (!_editUserVM.HasPassword)
                _pwdOldPassword.InteractionState.ParameterValue = ComponentState.ForceDisabled;

            var queryUser = NavigationManager.GetQueryString<string>("user")?.Base58ToUTF8OrNull()?.JsonDeserializeOrNull()?.To<LoginUserVM>();
            if (queryUser is not null)
            {
                queryUser.ReturnUrl = queryUser.ReturnUrl.Base58ToUTF8();
                queryUser.UserName = _editUserVM.UserName;

                await ConnectExternalLoginAsync(queryUser);
                return;
            }

            await SetControlStatesAsync(ComponentState.Enabled, _allControls);
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

        protected async Task BtnConnectExternalLogin_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken token)
        {
            await SetControlStatesAsync(ComponentState.Disabled, _allControls, sender);
            
            var url = $"{ConfigUtils.BackendBaseUrl}/api/account/externallogin";
            var qs = new OrderedDictionary<string, string>
            {
                ["provider"] = ((ExternalLoginVM) sender.Model.V).LoginProvider.ToLowerInvariant(),
                ["returnUrl"] = NavigationManager.Uri.BeforeFirstOrWhole("?").UTF8ToBase58(),
                ["rememberMe"] = AuthenticatedUser.RememberMe.ToString().ToLowerInvariant()
            };
            
            NavigationManager.NavigateTo($"{url}?{qs.ToQueryString()}", true);
        }

        private async Task ConnectExternalLoginAsync(LoginUserVM queryUser)
        {
            var btnCurrentExternalLoginConnect = (await ComponentsByTypeAsync<MyButtonBase>()).Single(b => (b.Model?.V as ExternalLoginVM)?.LoginProvider.EqualsIgnoreCase_(queryUser.ExternalProvider) == true);
            await SetControlStatesAsync(ComponentState.Disabled, _allControls, btnCurrentExternalLoginConnect);
            var connectResp = await AccountClient.ConnectExternalLoginAsync(_editUserVM, queryUser);
            if (connectResp.IsError)
            {
                await PromptMessageAsync(NotificationType.Error, connectResp.Message);
                await SetControlStatesAsync(ComponentState.Enabled, _allControls);
                return;
            }
            
            _editUserVM = connectResp.Result;

            await PromptMessageAsync(NotificationType.Success, connectResp.Message);
            await StateHasChangedAsync(true); // to re-loop providers
            _allControls = GetInputControls(); // to update providers controls to remove the disconnected one
            await SetControlStatesAsync(ComponentState.Enabled, _allControls);
        }

        protected async Task BtnDisconnectExternalLogin_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken token)
        {
            await SetControlStatesAsync(ComponentState.Disabled, _allControls, sender);
            _editUserVM.ExternalProviderToDisconnect = ((ExternalLoginVM) sender.Model.V).LoginProvider.ToLowerInvariant();
            var disconnectResp = await AccountClient.DisconnectExternalLoginAsync(_editUserVM);
            if (disconnectResp.IsError)
            {
                await PromptMessageAsync(NotificationType.Error, disconnectResp.Message);
                await SetControlStatesAsync(ComponentState.Enabled, _allControls);
                return;
            }

            _editUserVM = disconnectResp.Result;

            await PromptMessageAsync(NotificationType.Success, disconnectResp.Message);
            await StateHasChangedAsync(true); // to re-loop providers
            _allControls = GetInputControls(); // to update providers controls to remove the disconnected one
            await SetControlStatesAsync(ComponentState.Enabled, _allControls);
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
