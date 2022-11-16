using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils;
using CommonLib.Web.Source.Common.Components;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyEditFormComponent;
using CommonLib.Web.Source.Common.Components.MyModalComponent;
using CommonLib.Web.Source.Common.Components.MyPromptComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
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
        private MyComponentBase[] _allControls;
        private MyButtonBase _btnLogin;
        private OrderedDictionary<string, MyButtonBase> _btnExternalLogins;
        private MyButtonBase _btnCloseModal;
        private MyButtonBase _btnLogout;

        protected MyEditForm _editForm;
        protected MyEditContext _editContext;
        protected LoginUserVM _loginUserVM;
        
        public Task<IJSObjectReference> ModalModuleAsync => _modalModuleAsync ??= MyJsRuntime.ImportComponentOrPageModuleAsync(nameof(MyModal), NavigationManager, HttpClient);
        
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
                ReturnUrl = inheritedReturnUrl ?? NavigationManager.Uri.BeforeFirstOrWhole("?") // methods executed on load events (initialised, afterrender, parametersset) can't raise `AuthenticationStateChanged` Event because it would cause an infinite loop when the Control State changes
            };
            _editContext ??= new MyEditContext(_loginUserVM);
            _btnExternalLogins ??= new OrderedDictionary<string, MyButtonBase>();
            await Task.CompletedTask;
        }
        
        protected override async Task OnAfterFirstRenderAsync()
        {
            if (!await EnsureAuthenticationPerformedAsync(true, false))
                return;

            _loginUserVM.ExternalLogins = (await AccountClient.GetExternalAuthenticationSchemesAsync()).Result;
            
            await StateHasChangedAsync(); // to re-render External Login Buttons and get their references using @ref in .razor file
            SetControls();
            await SetControlStatesAsync(ButtonState.Disabled, _allControls); // disable External Login Buttons
            
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

                await SetControlStatesAsync(ButtonState.Enabled, _allControls);
            }
            else
            {
                AuthenticatedUser.Avatar = (await AccountClient.GetUserAvatarByNameAsync(AuthenticatedUser.UserName))?.Result;
                Mapper.Map(AuthenticatedUser, _loginUserVM);
                await SetControlStatesAsync(ButtonState.Enabled, _allControls);
            }
        }
        
        protected override async Task OnAfterRenderAsync(bool isFirstRender)
        {
            if (isFirstRender || IsDisposed)
                return;

            if (await EnsureAuthenticationChangedAsync(false, false))
            {
                SetControls();
                await SetControlStatesAsync(ButtonState.Enabled, _allControls);
            }
        }

        private async Task ExternalLoginAuthorizeAsync(LoginUserVM queryUser)
        {
            await SetControlStatesAsync(ButtonState.Disabled, _allControls, _btnExternalLogins[queryUser.ExternalProvider]);
            var externalLoginResponse = await AccountClient.ExternalLoginAuthorizeAsync(_loginUserVM);
            if (externalLoginResponse.IsError)
            {
                await PromptMessageAsync(NotificationType.Error, externalLoginResponse.Message);
                await SetControlStatesAsync(ButtonState.Enabled, _allControls);
                return;
            }

            Mapper.Map(externalLoginResponse.Result, _loginUserVM);
            await PromptMessageAsync(NotificationType.Success, externalLoginResponse.Message);
            await (await ComponentByClassAsync<MyModalBase>("my-login-modal")).HideModalAsync();
            _btnExternalLogins[queryUser.ExternalProvider].State.ParameterValue = ButtonState.Disabled;
            await EnsureAuthenticationPerformedAsync(false, false);
            SetControls();
            await SetControlStatesAsync(ButtonState.Enabled, _allControls);
            if (!_loginUserVM.IsConfirmed)
                NavigationManager.NavigateTo($"/Account/ConfirmEmail?{GetConfirmEmailNavQueryStrings()}");
            else if (!externalLoginResponse.Result.ReturnUrl.IsNullOrWhiteSpace())
                NavigationManager.NavigateTo(_loginUserVM.ReturnUrl);
        }

        protected async Task FormLogin_ValidSubmitAsync()
        {
            await SetControlStatesAsync(ButtonState.Disabled, _allControls, _btnLogin);
            var loginResult = await AccountClient.LoginAsync(_loginUserVM);
            if (loginResult.IsError)
            {
                await PromptMessageAsync(NotificationType.Error, loginResult.Message); // prompt from modal
                await SetControlStatesAsync(ButtonState.Enabled, _allControls);
                return;
            }

            await PromptMessageAsync(NotificationType.Success, loginResult.Message);
            if (!loginResult.Result.ReturnUrl.IsNullOrWhiteSpace())
                NavigationManager.NavigateTo(loginResult.Result.ReturnUrl);

            await (await ComponentByClassAsync<MyModalBase>("my-login-modal")).HideModalAsync();
            if (await EnsureAuthenticationChangedAsync(true, false))
            {
                SetControls();
                await SetControlStatesAsync(ButtonState.Enabled, _allControls);
            }
        }

        protected async Task BtnExternalLogin_ClickAsync(MouseEventArgs e, string provider)
        {
            await SetControlStatesAsync(ButtonState.Disabled, _allControls, _btnExternalLogins[provider]);

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

        protected async Task BtnSignUp_ClickAsync(MouseEventArgs e) => await OnSignUpClick.InvokeAsync(e).ConfigureAwait(false);

        protected async Task BtnSignIn_ClickAsync() => await _editForm.SubmitAsync();

        protected async Task BtnSignOut_ClickAsync()
        {
            await SetControlStatesAsync(ButtonState.Disabled, _allControls, _btnLogout);
            var logoutResult = await AccountClient.LogoutAsync();
            if (logoutResult.IsError)
            {
                await PromptMessageAsync(NotificationType.Error, logoutResult.Message);
                await SetControlStatesAsync(ButtonState.Enabled, _allControls);
                return;
            }

            await PromptMessageAsync(NotificationType.Success, logoutResult.Message);
            await (await ComponentByClassAsync<MyModalBase>("my-login-modal")).HideModalAsync();
            _loginUserVM.UserName = null;
            _loginUserVM.Password = null;

            if (await EnsureAuthenticationChangedAsync(true, false))
            {
                SetControls();
                await SetControlStatesAsync(ButtonState.Enabled, _allControls);
            }
        }

        private void SetControls()
        {
            if (IsDisposed)
                return;

            _btnCloseModal = Parent.Parent.Children.OfType<MyButtonBase>().Single(d => d.Classes.Contains("my-close"));
            _allControls = GetInputControls().Append_(_btnCloseModal).ToArray();

            _btnLogin = _allControls.OfType<MyButtonBase>().SingleOrDefault(b => b.Value.V == "Sign In");
            _btnExternalLogins = _allControls.OfType<MyButtonBase>().Where(b => b.Value.V.In(_loginUserVM.ExternalLogins.Select(l => l.DisplayName))).ToOrderedDictionary(b => b.Value.V, b => b);
            _btnLogout = _allControls.OfType<MyButtonBase>().SingleOrDefault(b => b.Value.V == "Sign Out");
        }

        protected async Task BtnResetPassword_ClickAsync(MouseEventArgs e) => await OnResetPasswordClick.InvokeAsync(e).ConfigureAwait(false);

        protected async Task BtnEdit_ClickAsync(MouseEventArgs e) => await OnEditClick.InvokeAsync(e).ConfigureAwait(false);

        private string GetConfirmEmailNavQueryStrings()
        {
            return new OrderedDictionary<string, string>
            {
                [nameof(_loginUserVM.Email).PascalCaseToCamelCase()] = _loginUserVM.Email?.UTF8ToBase58(false),
                [nameof(_loginUserVM.ReturnUrl).PascalCaseToCamelCase()] = _loginUserVM.ReturnUrl?.UTF8ToBase58(false),
            }.Where(kvp => kvp.Value != null).ToQueryString();
        }
    }
}
