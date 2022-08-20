using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils;
using CommonLib.Web.Source.Common.Components;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyCheckBoxComponent;
using CommonLib.Web.Source.Common.Components.MyEditContextComponent;
using CommonLib.Web.Source.Common.Components.MyEditFormComponent;
using CommonLib.Web.Source.Common.Components.MyFluentValidatorComponent;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Components.MyModalComponent;
using CommonLib.Web.Source.Common.Components.MyPasswordInputComponent;
using CommonLib.Web.Source.Common.Components.MyPromptComponent;
using CommonLib.Web.Source.Common.Components.MyTextInputComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.ViewModels.Account;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MoreLinq;
using Truncon.Collections;

namespace CommonLib.Web.Source.Common.Pages.Account
{
    public class LoginBase : MyComponentBase
    {
        protected MyEditForm _editForm;
        protected MyEditContext _editContext;
        protected MyButtonBase _btnLogin;
        protected MyButtonBase _btnRegister;
        protected MyButtonBase _btnResetPassword;
        protected MyButtonBase _btnDismiss;
        protected OrderedDictionary<string, MyButtonBase> _btnExternalLogins;
        protected MyCheckBoxBase _cbRememberMe;
        protected MyTextInputBase _txtUserName;
        protected MyPasswordInputBase _txtPassword;
        protected LoginUserVM _loginUserVM;
        
        [CascadingParameter]
        public MyButtonBase BtnCloseModal { get; set; }

        [Parameter] 
        public EventCallback<MouseEventArgs> OnClick { get; set; }

        [Inject]
        public IMapper Mapper { get; set; }
        
        protected override async Task OnInitializedAsync()
        {
            _loginUserVM ??= new LoginUserVM
            {
                ReturnUrl = $"{NavigationManager.GetQueryString<string>("returnUrl")?.Base58ToUTF8()?.BeforeFirstOrWhole("?")}?keepPrompt=true", // methods executed on load events (initialised, afterrender, parametersset) can't raise `AuthenticationStateChanged` Event because it would cause an infinite loop when the Control State changes
            };
            _editContext ??= new MyEditContext(_loginUserVM);
            _btnExternalLogins ??= new OrderedDictionary<string, MyButtonBase>();
            await Task.CompletedTask;
        }

        protected override async Task OnParametersSetAsync()
        {
            await Task.CompletedTask;
        }

        protected override async Task OnAfterFirstRenderAsync()
        {
            await SetControlStatesAsync(ButtonState.Disabled);
            
            AuthenticatedUser = (await AccountClient.GetAuthenticatedUserAsync()).Result;
            _loginUserVM.ExternalLogins = (await AccountClient.GetExternalAuthenticationSchemesAsync()).Result;

            await StateHasChangedAsync(); // to re-render External Login Buttons and get their references using @ref in .razor file
            await SetControlStatesAsync(ButtonState.Disabled); // disable External Login Buttons
            
            var queryUser = NavigationManager.GetQueryString<string>("user")?.Base58ToUTF8OrNull()?.JsonDeserializeOrNull()?.To<LoginUserVM>();

            if (!IsAuthenticated()) // try to authorize with what is present in queryStrings, possibly from an external provider
            {
                var remoteStatus = NavigationManager.GetQueryString<string>("remoteStatus")?.ToEnumN<NotificationType>();
                var remoteMessage = NavigationManager.GetQueryString<string>("remoteMessage");

                if (remoteStatus == NotificationType.Error)
                {
                    await PromptMessageAsync(remoteStatus.ToEnum<NotificationType>(), remoteMessage?.Base58ToUTF8OrNull() ?? "Unable to Log In with an External provider");
                    return;
                }

                if (queryUser != null && !IsAuthenticated())
                {
                    _btnExternalLogins[queryUser.ExternalProvider].State.ParameterValue = ButtonState.Loading;
                    await StateHasChangedAsync();

                    await ExternalLoginAuthorizeAsync(queryUser);
                    return;
                }

                await SetControlStatesAsync(ButtonState.Enabled);
                return;
            }

            // TODO: instead update the NavBar?
            //var keepPrompt = NavigationManager.GetQueryString<bool?>("keepPrompt") ?? false;
            //if (queryUser == null && !keepPrompt) // if we have just logged in with an external provider then we want to leave the provider message visible, otherwise we let the user know he is logged in
            //    await PromptMessageAsync(NotificationType.Success, $"You are logged in as \"{AuthenticatedUser.UserName}\"");

            await SetControlStatesAsync(ButtonState.Enabled);
        }

        private async Task ExternalLoginAuthorizeAsync(LoginUserVM queryUser)
        {
            await SetControlStatesAsync(ButtonState.Disabled, _btnExternalLogins[queryUser.ExternalProvider]);

            var externalSchemes = _loginUserVM.ExternalLogins.ToList(); // would be overwritten by automapper;
            Mapper.Map(queryUser, _loginUserVM);
            _loginUserVM.ExternalLogins = externalSchemes;
            var externalLoginResult = await AccountClient.ExternalLoginAuthorizeAsync(_loginUserVM);
            if (externalLoginResult.IsError)
            {
                await PromptMessageAsync(NotificationType.Error, externalLoginResult.Message);
                await SetControlStatesAsync(ButtonState.Enabled);
                return;
            }

            NavigationManager.NavigateTo(externalLoginResult.Result.ReturnUrl);
            await PromptMessageAsync(NotificationType.Success, externalLoginResult.Message);
            UserAuthStateProvider.StateChanged();
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

            _btnLogin.State.ParameterValue = ButtonState.Disabled;
            _btnDismiss.State.ParameterValue = ButtonState.Enabled;
            BtnCloseModal.State.ParameterValue = ButtonState.Enabled;
            await BtnCloseModal.NotifyParametersChangedAsync().StateHasChangedAsync(true); 
            await StateHasChangedAsync();
            await PromptMessageAsync(NotificationType.Success, loginResult.Message);
            await (await ComponentByClassAsync<MyModalBase>("my-login-modal")).HideModalAsync();
            NavigationManager.NavigateTo(loginResult.Result.ReturnUrl);
            UserAuthStateProvider.StateChanged();
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

            var otherButtons = new[] { _btnLogin, _btnRegister, _btnDismiss, _btnResetPassword, BtnCloseModal }.Concat(_btnExternalLogins.Values).Except(btnLoading).ToArray();
            foreach (var btn in otherButtons)
                btn.State.ParameterValue = otherControlsState;

            await BtnCloseModal.NotifyParametersChangedAsync().StateHasChangedAsync(true); // `BtnCloseModal` is not part of `Login.razor` so `State` needs to be changed manually

            MyInputBase[] allInputs = { _cbRememberMe, _txtUserName, _txtPassword };
            foreach (var input in allInputs)
                input.State.ParameterValue = otherControlsState == ButtonState.Enabled ? InputState.Enabled : InputState.Disabled;
            
            await StateHasChangedAsync(true);
        }

        protected async Task BtnSignUp_ClickAsync(MouseEventArgs e) => await OnClick.InvokeAsync(e).ConfigureAwait(false);

        protected async Task BtnSignIn_ClickAsync() => await _editForm.SubmitAsync();
    }
}
