using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyCssGridItemComponent;
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
using Nethereum.Hex.HexTypes;
using Nethereum.UI;
using Nethereum.Web3;
using Truncon.Collections;

namespace CommonLib.Web.Source.Common.Pages.Account
{
    public class LoginBase : MyComponentBase, IDisposable
    {
        private Task<IJSObjectReference> _modalModuleAsync;
        private MyComponentBase[] _allControls;
        private MyButtonBase _btnLogin;
        private OrderedDictionary<string, MyButtonBase> _btnExternalLogins;
        private OrderedDictionary<string, MyButtonBase> _btnWalletLogins;
        private MyButtonBase _btnCloseModal;
        private MyButtonBase _btnLogout;
        private IEthereumHostProvider _ethereumHostProvider;
        private IWeb3 _web3;

        protected MyEditForm _editForm;
        protected MyEditContext _editContext;
        protected LoginUserVM _loginUserVM;
        protected MyCssGridItemBase _giAvatarContainer;
        
        public Task<IJSObjectReference> ModalModuleAsync => _modalModuleAsync ??= MyJsRuntime.ImportComponentOrPageModuleAsync(nameof(MyModal), NavigationManager, HttpClient);
        
        [Parameter] 
        public EventCallback<MouseEventArgs> OnSignUpClick { get; set; }

        [Parameter] 
        public EventCallback<MouseEventArgs> OnResetPasswordClick { get; set; }
        
        [Parameter] 
        public MyAsyncEventHandler<MyButtonBase, MouseEventArgs> OnEditClick { get; set; }
        
        [Inject]
        public IJQueryService JQuery { get; set; }
        
        [Inject]
        public SelectedEthereumHostProviderService SelectedEthereumHost { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var inheritedReturnUrl = NavigationManager.GetQueryString<string>("returnUrl")?.Base58ToUTF8()?.BeforeFirstOrWhole("?");

            _loginUserVM ??= new LoginUserVM
            {
                ReturnUrl = inheritedReturnUrl ?? NavigationManager.Uri.BeforeFirstOrWhole("?") // methods executed on load events (initialised, afterrender, parametersset) can't raise `AuthenticationStateChanged` Event because it would cause an infinite loop when the Control State changes
            };
            _editContext ??= new MyEditContext(_loginUserVM);
            _btnExternalLogins ??= new OrderedDictionary<string, MyButtonBase>();
            _btnWalletLogins ??= new OrderedDictionary<string, MyButtonBase>();

            _ethereumHostProvider = SelectedEthereumHost.SelectedHost;
            _ethereumHostProvider.SelectedAccountChanged += SelectedEthereumHost_SelectedAccountChangedAsync;
            _ethereumHostProvider.NetworkChanged += SelectedEthereumHost_NetworkChangedAsync;
            _ethereumHostProvider.EnabledChanged += SelectedEthereumHost_ChangedAsync;
            _web3 = await _ethereumHostProvider.GetWeb3Async();
            
            await Task.CompletedTask;
        }

        protected override async Task OnAfterFirstRenderAsync()
        {
            if (!await EnsureAuthenticationPerformedAsync(true, false))
                return;

            _loginUserVM.ExternalLogins = (await AccountClient.GetExternalAuthenticationSchemesAsync()).Result;
            // changing state will rerender the component but after render will be blocked by semaphore and eexecuted only after this function
            await StateHasChangedAsync(true); // to re-render External Login Buttons and get their references using @ref in .razor file
            await SetControlsAsync();
            await SetControlStatesAsync(ComponentState.Disabled, _allControls, null, ChangeRenderingStateMode.AllSpecified); // disable External Login Buttons

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

                if (queryUser is not null)
                {
                    await (await ModalModuleAsync).InvokeVoidAsync("blazor_Modal_ShowAsync", ".my-login-modal", false).ConfigureAwait(false); //await (await ComponentByClassAsync<MyModalBase>("my-login-modal")).ShowModalAsync(false); // it isn't guranteed that at this point Modal is loaded to ComponentsCache
                    queryUser.ExternalLogins = _loginUserVM.ExternalLogins.ToList();
                    queryUser.UserName = (await AccountClient.FindUserByEmailAsync(queryUser.Email)).Result?.UserName ?? queryUser.UserName;
                    Mapper.Map(queryUser, _loginUserVM);

                    _btnExternalLogins[queryUser.ExternalProvider].InteractionState.ParameterValue = ComponentState.Loading;
                    await StateHasChangedAsync(true);

                    await ExternalLoginAuthorizeAsync(queryUser);
                    return;
                }

                await SetControlStatesAsync(ComponentState.Enabled, _allControls, null, ChangeRenderingStateMode.AllSpecified);
            }
            else
            {
                AuthenticatedUser.Avatar = (await AccountClient.GetUserAvatarByNameAsync(AuthenticatedUser.UserName))?.Result;
                Mapper.Map(AuthenticatedUser, _loginUserVM);
                await SetControlStatesAsync(ComponentState.Enabled, _allControls, null, ChangeRenderingStateMode.AllSpecified);
            }
        }

        // if enabled then it might trigger in the middle of authentication,
        // if disabled it may leave edit and logout button disabled after authentication
        // if changed to AuthStateChanged then it might not trigger when it should because sth else already refreshed the logged in panel
        protected override async Task OnAfterRenderAsync(bool isFirstRender, bool authUserChanged)
        {
            //if (isFirstRender || IsDisposed || _allControls.Any(c => c?.InteractionState?.V.IsLoadingOrForceLoading == true))
            //    return;

            //if (await EnsureAuthenticationPerformedAsync(false, false)) // not changed because change may be frontrun by components updating it earlier
            //{
            //    SetControls();
            //    await SetControlStatesAsync(ComponentState.Enabled, _allControls, null, ChangeRenderingStateMode.AllSpecified);
            //}
        }

        private async Task ExternalLoginAuthorizeAsync(LoginUserVM queryUser)
        {
            await SetControlStatesAsync(ComponentState.Disabled, _allControls, _btnExternalLogins[queryUser.ExternalProvider]);
            var externalLoginResp = await AccountClient.ExternalLoginAuthorizeAsync(_loginUserVM);
            if (externalLoginResp.IsError)
            {
                await PromptMessageAsync(NotificationType.Error, externalLoginResp.Message);
                await SetControlStatesAsync(ComponentState.Enabled, _allControls);
                return;
            }

            Mapper.Map(externalLoginResp.Result, _loginUserVM);
            await PromptMessageAsync(NotificationType.Success, externalLoginResp.Message);
            await HideLoginModalAsync();
            _btnExternalLogins[queryUser.ExternalProvider].InteractionState.ParameterValue = ComponentState.Disabled;
           
            await EnsureAuthenticationPerformedAsync(false, true, true);
            await SetControlsAsync();
            await SetControlStatesAsync(ComponentState.Enabled, _allControls);
           
            if (!_loginUserVM.IsConfirmed && _loginUserVM.Email is not null) // email is null if for instance external login profile was connected to an account that was previously using only wallet login
                NavigationManager.NavigateTo($"/Account/ConfirmEmail?{GetConfirmEmailNavQueryStrings()}"); // login is in the modal, page should be changed if email confirmation is required
            else if (!externalLoginResp.Result.ReturnUrl.IsNullOrWhiteSpace())
                NavigationManager.NavigateTo(_loginUserVM.ReturnUrl); // this is to remove the `loginUserVm` query string from the address
        }

        protected async Task FormLogin_ValidSubmitAsync()
        {
            await SetControlStatesAsync(ComponentState.Disabled, _allControls, _btnLogin, ChangeRenderingStateMode.AllSpecified);
            var loginResult = await AccountClient.LoginAsync(_loginUserVM);
            if (loginResult.IsError)
            {
                await PromptMessageAsync(NotificationType.Error, loginResult.Message); // prompt from modal
                await SetControlStatesAsync(ComponentState.Enabled, _allControls);
                return;
            }

            await PromptMessageAsync(NotificationType.Success, loginResult.Message);
            //if (!loginResult.Result.ReturnUrl.IsNullOrWhiteSpace()) // login is in the modal, page should be changed if email confirmation is required
            //    NavigationManager.NavigateTo(loginResult.Result.ReturnUrl);

            await HideLoginModalAsync();

            if (await EnsureAuthenticationPerformedAsync(true, true, true)) // not changed because change may be frontrun by components updating it earlier
            {
                await SetControlsAsync();
                await SetControlStatesAsync(ComponentState.Enabled, _allControls, null, ChangeRenderingStateMode.AllSpecified);
            }
        }

        protected async Task BtnExternalLogin_ClickAsync(MouseEventArgs e, string provider)
        {
            await SetControlStatesAsync(ComponentState.Disabled, _allControls, _btnExternalLogins[provider]);
            _loginUserVM.ReturnUrl = NavigationManager.Uri;
            _loginUserVM.ExternalProvider = provider;
            _loginUserVM.Mode = ExternalLoginUsageMode.Login;
            var url = $"{ConfigUtils.BackendBaseUrl}/api/account/externallogin";
            NavigationManager.NavigateTo($"{url}?user={_loginUserVM.JsonSerialize().UTF8ToBase58()}", true);
        }
        
        protected async Task BtnWalletLogin_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken token)
        {
            _loginUserVM.WalletProvider = sender.Value.V; 
            await SetControlStatesAsync(ComponentState.Disabled, _allControls, sender);
            
            if (_loginUserVM.WalletProvider.EqualsIgnoreCase("Metamask"))
            {
                var isHostProviderAvailable = await _ethereumHostProvider.CheckProviderAvailabilityAsync();
                if (!isHostProviderAvailable)
                {
                    await PromptMessageAsync(NotificationType.Error, "Metamask is not installed");
                    await SetControlStatesAsync(ComponentState.Enabled, _allControls);
                    return;
                }

                var enableWalletResp = await _ethereumHostProvider.TryEnableProviderAsync();
                if (enableWalletResp.IsError)
                {
                    await PromptMessageAsync(NotificationType.Error, "Metamask was not enabled");
                    await SetControlStatesAsync(ComponentState.Enabled, _allControls);
                    return;
                }
                _loginUserVM.WalletAddress = enableWalletResp.Result;

                var walletSignatureResp = await _web3.Eth.AccountSigning.PersonalSign.TrySendRequestAsync($"Proving ownership of wallet: \"{_loginUserVM.WalletAddress}\"");
                if (walletSignatureResp.IsError)
                {
                    await PromptMessageAsync(NotificationType.Error, walletSignatureResp.Message);
                    await SetControlStatesAsync(ComponentState.Enabled, _allControls);
                    return;
                }
                _loginUserVM.WalletSignature = walletSignatureResp.Result;
            }
            else
            {
                await PromptMessageAsync(NotificationType.Error, $"{ _loginUserVM.WalletProvider} Wallet Provider is not supported");
                await SetControlStatesAsync(ComponentState.Enabled, _allControls);
                return;
            }
            
            var walletLoginResp = await AccountClient.WalletLoginAsync(_loginUserVM);
            if (walletLoginResp.IsError)
            {
                await PromptMessageAsync(NotificationType.Error, walletLoginResp.Message); // prompt from modal
                await SetControlStatesAsync(ComponentState.Enabled, _allControls);
                return;
            }

            await PromptMessageAsync(NotificationType.Success, walletLoginResp.Message);
            await HideLoginModalAsync();

            if (await EnsureAuthenticationPerformedAsync(true, true, true)) // not changed because change may be frontrun by components updating it earlier
            {
                await SetControlsAsync();
                await SetControlStatesAsync(ComponentState.Enabled, _allControls);
            }
        }
        
        private async Task SelectedEthereumHost_ChangedAsync(bool isEnabled)
        {
            if (isEnabled)
                _loginUserVM.WalletChainId = (int)(await _web3.Eth.ChainId.SendRequestAsync()).Value;
        }

        private async Task SelectedEthereumHost_NetworkChangedAsync(long chainId)
        {
            _loginUserVM.WalletChainId = (int)chainId;
            await Task.CompletedTask;
        }

        private async Task SelectedEthereumHost_SelectedAccountChangedAsync(string address)
        {
            _loginUserVM.WalletAddress = address;
            _loginUserVM.WalletChainId = (int)(await _web3.Eth.ChainId.SendRequestAsync()).Value;
        }
        
        protected async Task BtnSignUp_ClickAsync(MouseEventArgs e) => await OnSignUpClick.InvokeAsync(e).ConfigureAwait(false);

        protected async Task BtnSignIn_ClickAsync() => await _editForm.SubmitAsync();

        protected async Task BtnSignOut_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken token)
        {
            await SetControlStatesAsync(ComponentState.Disabled, _allControls, _btnLogout);
            var logoutResult = await AccountClient.LogoutAsync();
            if (logoutResult.IsError)
            {
                await PromptMessageAsync(NotificationType.Error, logoutResult.Message);
                await SetControlStatesAsync(ComponentState.Enabled, _allControls, null, ChangeRenderingStateMode.AllSpecified);
                return;
            }

            await PromptMessageAsync(NotificationType.Success, logoutResult.Message);
            _loginUserVM.UserName = null;
            _loginUserVM.Password = null;
            //AuthenticatedUser = AuthenticateUserVM.NotAuthenticated;

            await HideLoginModalAsync();

            if (await EnsureAuthenticationPerformedAsync(true, true)) // not changed because change may be frontrun by components updating it earlier
            {
                await SetControlsAsync();
                await SetControlStatesAsync(ComponentState.Enabled, _allControls, null, ChangeRenderingStateMode.AllSpecified);
            }
        }

        private async Task SetControlsAsync()
        {
            if (IsDisposed)
                return;

            var externalLogins = _loginUserVM?.ExternalLogins ?? (await AccountClient.GetExternalAuthenticationSchemesAsync()).Result;

            _btnCloseModal = Parent.Parent.Children.OfType<MyButtonBase>().Single(d => d.Classes.Contains("my-close"));
            _allControls = GetInputControls().Append_(_btnCloseModal).AppendIfNotNull(_giAvatarContainer).ToArray();

            _btnLogin = _allControls.OfType<MyButtonBase>().SingleOrDefault(b => b.Value.V == "Sign In");
            _btnExternalLogins = _allControls.OfType<MyButtonBase>().Where(b => b.Value.V.In(_loginUserVM.ExternalLogins.Select(l => l.DisplayName))).ToOrderedDictionary(b => b.Value.V, b => b);
            _btnWalletLogins = _allControls.OfType<MyButtonBase>().Where(b => b.Value.V.In(_loginUserVM.WalletLogins.Select(l => l))).ToOrderedDictionary(b => b.Value.V, b => b);
            _btnLogout = _allControls.OfType<MyButtonBase>().SingleOrDefault(b => b.Value.V == "Sign Out");
        }

        protected async Task BtnResetPassword_ClickAsync(MouseEventArgs e) => await OnResetPasswordClick.InvokeAsync(e).ConfigureAwait(false);

        protected async Task BtnEdit_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken token) => await OnEditClick.InvokeAsync(sender, e, token).ConfigureAwait(false);

        private string GetConfirmEmailNavQueryStrings()
        {
            return new OrderedDictionary<string, string>
            {
                [nameof(_loginUserVM.Email).PascalCaseToCamelCase()] = _loginUserVM.Email?.UTF8ToBase58(false),
                [nameof(_loginUserVM.ReturnUrl).PascalCaseToCamelCase()] = _loginUserVM.ReturnUrl?.UTF8ToBase58(false),
            }.Where(kvp => kvp.Value != null).ToQueryString();
        }

        public void Dispose()
        {
            _ethereumHostProvider.SelectedAccountChanged -= SelectedEthereumHost_SelectedAccountChangedAsync;
            _ethereumHostProvider.NetworkChanged -= SelectedEthereumHost_NetworkChangedAsync;
            _ethereumHostProvider.EnabledChanged -= SelectedEthereumHost_ChangedAsync;
        }
    }
}
