using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.TypeUtils;
using CommonLib.Web.Source.Common.Components;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyCheckBoxComponent;
using CommonLib.Web.Source.Common.Components.MyEditContextComponent;
using CommonLib.Web.Source.Common.Components.MyEditFormComponent;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Components.MyModalComponent;
using CommonLib.Web.Source.Common.Components.MyPasswordInputComponent;
using CommonLib.Web.Source.Common.Components.MyPromptComponent;
using CommonLib.Web.Source.Common.Components.MyTextInputComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Models;
using CommonLib.Web.Source.Services.Interfaces;
using CommonLib.Web.Source.ViewModels.Account;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Truncon.Collections;

namespace CommonLib.Web.Source.Common.Pages.Account
{
    public class LoginBase : MyComponentBase
    {
        private Task<IJSObjectReference> _modalModuleAsync;

        protected MyEditForm _editForm;
        protected MyEditContext _editContext;
        protected MyButtonBase _btnLogin;
        protected MyButtonBase _btnLogout;
        protected MyButtonBase _btnRegister;
        protected MyButtonBase _btnResetPassword;
        protected MyButtonBase _btnDismiss;
        protected MyButtonBase _btnEdit;
        protected OrderedDictionary<string, MyButtonBase> _btnExternalLogins;
        protected MyCheckBoxBase _cbRememberMe;
        protected MyTextInputBase _txtUserName;
        protected MyPasswordInputBase _txtPassword;
        protected LoginUserVM _loginUserVM;
        protected BlazorParameter<ButtonState?> _btnLogoutState;
        protected BlazorParameter<ButtonState?> _btnEditState;
       
        public Task<IJSObjectReference> ModalModuleAsync => _modalModuleAsync ??= MyJsRuntime.ImportComponentOrPageModuleAsync(nameof(MyModal), NavigationManager, HttpClient);

        [CascadingParameter]
        public MyButtonBase BtnCloseModal { get; set; }

        [Parameter] 
        public EventCallback<MouseEventArgs> OnSignUpClick { get; set; }

        [Parameter] 
        public EventCallback<MouseEventArgs> OnResetPasswordClick { get; set; }
        
        [Parameter] 
        public EventCallback<MouseEventArgs> OnEditClick { get; set; }
        
        [Inject]
        public IJQueryService JQuery { get; set; }
        
        protected override async Task OnInitializedAsync()
        {
            var inheritedReturnUrl = NavigationManager.GetQueryString<string>("returnUrl")?.Base58ToUTF8()?.BeforeFirstOrWhole("?");

            _loginUserVM ??= new LoginUserVM
            {
                //RememberMe = true,
                ReturnUrl = inheritedReturnUrl // methods executed on load events (initialised, afterrender, parametersset) can't raise `AuthenticationStateChanged` Event because it would cause an infinite loop when the Control State changes
            };
            _editContext ??= new MyEditContext(_loginUserVM);
            _btnExternalLogins ??= new OrderedDictionary<string, MyButtonBase>();
            await Task.CompletedTask;
        }

        protected override async Task OnParametersSetAsync()
        {
            if (FirstParamSetup)
            {
                _btnLogoutState = ButtonState.Disabled;
                _btnEditState = ButtonState.Disabled;
            }

            await Task.CompletedTask;
        }

        protected override async Task OnAfterFirstRenderAsync()
        {
            await SetControlStatesAsync(ButtonState.Disabled);
            
            if (!await EnsureAuthenticationPerformedAsync())
                return;

            _loginUserVM.ExternalLogins = (await AccountClient.GetExternalAuthenticationSchemesAsync()).Result;
            
            await StateHasChangedAsync(); // to re-render External Login Buttons and get their references using @ref in .razor file
            await SetControlStatesAsync(ButtonState.Disabled); // disable External Login Buttons
            
            if (!HasAuthenticationStatus(AuthStatus.Authenticated)) // try to authorize with what is present in queryStrings, possibly from an external provider
            {
                var remoteStatus = NavigationManager.GetQueryString<string>("remoteStatus")?.ToEnumN<NotificationType>();
                var remoteMessage = NavigationManager.GetQueryString<string>("remoteMessage");

                if (remoteStatus == NotificationType.Error)
                {
                    await PromptMessageAsync(remoteStatus.ToEnum<NotificationType>(), remoteMessage?.Base58ToUTF8OrNull() ?? "Unable to Log In with an External provider");
                    return;
                }

                var queryUser = NavigationManager.GetQueryString<string>("user")?.Base58ToUTF8OrNull()?.JsonDeserializeOrNull()?.To<LoginUserVM>();
                
                if (queryUser != null)
                {
                    await (await ModalModuleAsync).InvokeVoidAsync("blazor_Modal_ShowAsync", ".my-login-modal", false).ConfigureAwait(false); //await (await ComponentByClassAsync<MyModalBase>("my-login-modal")).ShowModalAsync(false); // it isn't guranteed that at this point Modal is loaded to ComponentsCache
                    queryUser.ReturnUrl = queryUser.ReturnUrl.Base58ToUTF8();
                    queryUser.ExternalLogins = _loginUserVM.ExternalLogins.ToList();
                    queryUser.UserName = (await AccountClient.FindUserByEmailAsync(queryUser.Email)).Result?.UserName;
                    Mapper.Map(queryUser, _loginUserVM);

                    _btnExternalLogins[queryUser.ExternalProvider].State.ParameterValue = ButtonState.Loading;
                    await StateHasChangedAsync();
                    
                    await ExternalLoginAuthorizeAsync(queryUser);
                    return;
                }

                await SetControlStatesAsync(ButtonState.Enabled);
            }
            else
            {
                Mapper.Map(AuthenticatedUser, _loginUserVM);

                await TaskUtils.WaitUntil(() => _btnLogout is not null);
                _btnLogout.State.ParameterValue = ButtonState.Enabled;
                _btnDismiss.State.ParameterValue = ButtonState.Enabled;
                _btnEdit.State.ParameterValue = ButtonState.Enabled;
                BtnCloseModal.State.ParameterValue = ButtonState.Enabled;
                await BtnCloseModal.NotifyParametersChangedAsync().StateHasChangedAsync(true); 
                await StateHasChangedAsync();
            }
        }

        protected override async Task OnAfterRenderAsync(bool isFirstRender)
        {
            if (isFirstRender)
                return;
            await EnsureAuthenticationPerformedAsync();
        }

        private async Task ExternalLoginAuthorizeAsync(LoginUserVM queryUser)
        {
            await SetControlStatesAsync(ButtonState.Disabled, _btnExternalLogins[queryUser.ExternalProvider]);
            
            var externalLoginResult = await AccountClient.ExternalLoginAuthorizeAsync(_loginUserVM);
            if (externalLoginResult.IsError)
            {
                await PromptMessageAsync(NotificationType.Error, externalLoginResult.Message);
                await SetControlStatesAsync(ButtonState.Enabled);
                return;
            }
            
            await PromptMessageAsync(NotificationType.Success, externalLoginResult.Message);
            if (!externalLoginResult.Result.ReturnUrl.IsNullOrWhiteSpace())
                NavigationManager.NavigateTo(externalLoginResult.Result.ReturnUrl);
            UserAuthStateProvider.StateChanged();

            await (await ComponentByClassAsync<MyModalBase>("my-login-modal")).HideModalAsync();
            
            _btnExternalLogins[queryUser.ExternalProvider].State.ParameterValue = ButtonState.Disabled;

            AuthenticatedUser = (await AccountClient.GetAuthenticatedUserAsync()).Result; // so IsAuthenticated returns correct value
            await StateHasChangedAsync(); // for Auth change to true so Render can swap Login btn for Logout, btnLogout is disabled on render to prevent it being momentarily available

            await TaskUtils.WaitUntil(() => _btnLogout is not null);
            _btnLogout.State.ParameterValue = ButtonState.Enabled; // using `_btnLogout.State.ParameterValue = ButtonState.Enabled;` if value is set on render would mean that after render it would always revert to "hard coded" disabled value so I have to use variable
            _btnDismiss.State.ParameterValue = ButtonState.Enabled;
            _btnEdit.State.ParameterValue = ButtonState.Enabled;
            BtnCloseModal.State.ParameterValue = ButtonState.Enabled;
            await BtnCloseModal.NotifyParametersChangedAsync().StateHasChangedAsync(true);
            await StateHasChangedAsync(true);
        }

        protected async Task FormLogin_ValidSubmitAsync()
        {
            await SetControlStatesAsync(ButtonState.Disabled, _btnLogin);
            var loginResult = await AccountClient.LoginAsync(_loginUserVM);
            if (loginResult.IsError)
            {
                await PromptMessageAsync(NotificationType.Error, loginResult.Message); // prompt from modal
                await SetControlStatesAsync(ButtonState.Enabled);
                return;
            }

            await PromptMessageAsync(NotificationType.Success, loginResult.Message);
            if (!loginResult.Result.ReturnUrl.IsNullOrWhiteSpace())
                NavigationManager.NavigateTo(loginResult.Result.ReturnUrl);
            UserAuthStateProvider.StateChanged();

            await (await ComponentByClassAsync<MyModalBase>("my-login-modal")).HideModalAsync();
            
            AuthenticatedUser = (await AccountClient.GetAuthenticatedUserAsync()).Result;
            await StateHasChangedAsync();

            _btnLogout.State.ParameterValue = ButtonState.Enabled;
            _btnEdit.State.ParameterValue = ButtonState.Enabled;
            _btnLogin.State.ParameterValue = ButtonState.Disabled;
            _btnDismiss.State.ParameterValue = ButtonState.Enabled;
            BtnCloseModal.State.ParameterValue = ButtonState.Enabled;
            await BtnCloseModal.NotifyParametersChangedAsync().StateHasChangedAsync(true);
            await StateHasChangedAsync(true);
        }

        protected async Task BtnExternalLogin_ClickAsync(MouseEventArgs e, string provider)
        {
            await SetControlStatesAsync(ButtonState.Disabled, _btnExternalLogins[provider]);

            _loginUserVM.ExternalProvider = provider;
            var url = $"{ConfigUtils.BackendBaseUrl}/api/account/externallogin";
            var qs = new Dictionary<string, string>
            {
                ["provider"] = _loginUserVM.ExternalProvider,
                ["returnUrl"] = _loginUserVM.ReturnUrl.UTF8ToBase58(),
                ["rememberMe"] = _loginUserVM.RememberMe.ToString().ToLowerInvariant()
            };
            
            NavigationManager.NavigateTo($"{url}?{qs.ToQueryString()}", true);
        }

        private async Task SetControlStatesAsync(ButtonState otherControlsState, MyButtonBase btnLoading = null)
        {
            if (btnLoading != null)
                btnLoading.State.ParameterValue = ButtonState.Loading;

            var otherButtons = new[] { _btnLogin, _btnLogout, _btnEdit, _btnRegister, _btnDismiss, _btnResetPassword, BtnCloseModal }.Concat(_btnExternalLogins.Values).Except(btnLoading).ToArray();
            foreach (var btn in otherButtons)
                if (btn != null)
                    btn.State.ParameterValue = otherControlsState;

            await BtnCloseModal.NotifyParametersChangedAsync().StateHasChangedAsync(true); // `BtnCloseModal` is not part of `Login.razor` so `State` needs to be changed manually

            MyInputBase[] allInputs = { _cbRememberMe, _txtUserName, _txtPassword };
            foreach (var input in allInputs)
                input.State.ParameterValue = otherControlsState == ButtonState.Enabled && !input.State.ParameterValue.IsForced ? InputState.Enabled : InputState.Disabled;
            
            await StateHasChangedAsync(true);
        }

        protected async Task BtnSignUp_ClickAsync(MouseEventArgs e) => await OnSignUpClick.InvokeAsync(e).ConfigureAwait(false);

        protected async Task BtnSignIn_ClickAsync() => await _editForm.SubmitAsync();

        protected async Task BtnSignOut_ClickAsync()
        {
            await SetControlStatesAsync(ButtonState.Disabled, _btnLogout);
            var logoutResult = await AccountClient.LogoutAsync();
            if (logoutResult.IsError)
            {
                await PromptMessageAsync(NotificationType.Error, logoutResult.Message);
                await SetControlStatesAsync(ButtonState.Enabled);
                return;
            }

            await PromptMessageAsync(NotificationType.Success, logoutResult.Message);
            UserAuthStateProvider.StateChanged();

            await (await ComponentByClassAsync<MyModalBase>("my-login-modal")).HideModalAsync();
            
            AuthenticatedUser = null;
            _loginUserVM.UserName = null;
            _loginUserVM.Password = null;
            await SetControlStatesAsync(ButtonState.Enabled);
        }

        protected async Task BtnResetPassword_ClickAsync(MouseEventArgs e) => await OnResetPasswordClick.InvokeAsync(e).ConfigureAwait(false);

        protected async Task BtnEdit_ClickAsync(MouseEventArgs e) => await OnEditClick.InvokeAsync(e).ConfigureAwait(false);
    }
}
