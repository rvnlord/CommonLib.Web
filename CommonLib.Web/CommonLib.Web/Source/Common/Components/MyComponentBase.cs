using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Blazored.SessionStorage;
using CommonLib.Web.Source.Common.Components.MyPromptComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Models;
using CommonLib.Web.Source.Services.Account;
using CommonLib.Web.Source.Services.Account.Interfaces;
using CommonLib.Web.Source.Services.Interfaces;
using CommonLib.Web.Source.ViewModels.Account;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.TypeUtils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components.MyModalComponent;
using CommonLib.Web.Source.Common.Components.MyNavBarComponent;
using CommonLib.Web.Source.Common.Pages.Shared;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using Truncon.Collections;
using Microsoft.AspNetCore.Http;
using MoreLinq;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Components.MyCheckBoxComponent;
using CommonLib.Web.Source.Common.Components.MyDropDownComponent;
using CommonLib.Web.Source.Common.Components.MyFileUploadComponent;
using CommonLib.Web.Source.Common.Components.MyIconComponent;
using CommonLib.Web.Source.Common.Components.MyInputGroupComponent;
using CommonLib.Web.Source.Common.Components.MyNavLinkComponent;
using CommonLib.Web.Source.Common.Components.MyPasswordInputComponent;
using CommonLib.Web.Source.Common.Components.MyTextInputComponent;
using CommonLib.Web.Source.Common.Components.MyMediaQueryComponent;
using CommonLib.Web.Source.Common.Components.MyProgressBarComponent;
using CommonLib.Web.Source.Common.Components.MyRadioButtonComponent;
using CommonLib.Web.Source.Common.Converters;
using CommonLib.Web.Source.Services.Admin.Interfaces;
using Keras;
using static NBitcoin.Protocol.Behaviors.ChainBehavior;
using Telerik.Blazor.Components.Common;

namespace CommonLib.Web.Source.Common.Components
{
    public abstract class MyComponentBase : IAsyncDisposable, IComponent, IHandleEvent, IHandleAfterRender, IEquatable<MyComponentBase> // LayoutComponentBase
    {
        private readonly RenderFragment _renderFragment;
        private RenderHandle _renderHandle;
        private bool _hasPendingQueuedRender;
        private bool _firstRenderAfterInit;
        private Task<IJSObjectReference> _moduleAsync;
        private Task<IJSObjectReference> _componentBaseModuleAsync;
        private Task<IJSObjectReference> _promptModuleAsync;
        private Task<IJSObjectReference> _inputModuleAsync;
        private bool _firstParamSetup;
        private bool _isInitialized;
        private BlazorParameter<ComponentState> _bpState;
        private bool _preventRenderOnce;

        [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Disposed asynchronously")]
        private readonly SemaphoreSlim _syncClasses = new(1, 1);
        [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Disposed asynchronously")]
        private readonly SemaphoreSlim _syncStyles = new(1, 1);
        [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Disposed asynchronously")]
        private readonly SemaphoreSlim _syncAttributes = new(1, 1);
        [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Disposed asynchronously")]
        private readonly SemaphoreSlim _syncStateChanged = new(1, 1);
        private readonly OrderedSemaphore _syncSettingParameters = new(1, 1);
        private readonly OrderedSemaphore _syncRender = new(1, 1);
        private readonly OrderedSemaphore _syncAfterSessionIdSet = new(1, 1);
        private readonly OrderedSemaphore _syncComponentCached = new(1, 1);
        private readonly OrderedSemaphore _syncAllComponentsCached = new(1, 1);
        //private OrderedSemaphore _syncComponentsCache => (_isCommonLayout ? (MyLayoutComponentBase)this : Layout)._syncComponentsCache;
        private MyPromptBase _prompt;
        private Guid _sessionId;
        private bool _sessionIdAlreadySet;
        private AuthenticateUserVM _authenticatedUser;
        private bool _isFirstRenderAfterAuthorization = true;

        protected OrderedDictionary<string, string> _prevAdditionalAttributes = new();
        protected Guid _guid { get; set; }
        protected string _id { get; set; }
        protected string _renderClasses { get; set; } // these properties prevents async component rendering from throwing if clicking sth fast would change the collection before it is iterated properly within the razor file
        protected string _renderStyle { get; set; }
        protected Dictionary<string, object> _renderAttributes { get; } = new();
        protected OrderedDictionary<string, string> _style { get; } = new();
        protected OrderedDictionary<string, string> _attributes { get; } = new();
        protected BlazorParameter<MyComponentBase> _bpParentToCascade { get; set; }

        public List<string> Classes { get; } = new();
        public Task<IJSObjectReference> ComponentBaseModuleAsync => _componentBaseModuleAsync ??= MyJsRuntime.ImportComponentOrPageModuleAsync(nameof(MyComponentBase).BeforeLast("Base"), NavigationManager, HttpClient);
        public Task<IJSObjectReference> ModuleAsync => _moduleAsync ??= MyJsRuntime.ImportComponentOrPageModuleAsync(GetType().BaseType?.Name.BeforeLast("Base"), NavigationManager, HttpClient);
        public Task<IJSObjectReference> PromptModuleAsync
        {
            get
            {
                if (IsCommonLayout)
                    return _promptModuleAsync ??= MyJsRuntime.ImportComponentOrPageModuleAsync(nameof(MyPromptBase).BeforeLast("Base"), NavigationManager, HttpClient);
                return LayoutParameter?.ParameterValue.PromptModuleAsync;
            }
        }
        public Task<IJSObjectReference> InputModuleAsync => _inputModuleAsync ??= MyJsRuntime.ImportComponentOrPageModuleAsync(nameof(MyInputBase).BeforeLast("Base"), NavigationManager, HttpClient);

        public bool IsRendered => !_firstRenderAfterInit;
        public bool IsDisposed { get; set; }
        public bool FirstParamSetup => _firstParamSetup;
        public bool IsRerendered { get; set; } // to be set manually on demand
        public bool IsCommonLayout
        {
            get
            {
                var type = GetType();
                return type.IsSubclassOf(typeof(MyLayoutComponentBase)) && type == typeof(MyLayoutComponent_Layout);
            }
        }

        public bool IsLayout
        {
            get
            {
                var type = GetType();
                return type.IsSubclassOf(typeof(MyLayoutComponentBase)) && type.Name.In("_Layout", "MainLayout");
            }
        }

        public bool IsPage => GetType().Namespace?.Split('.').Contains("Pages") == true;

        public Guid SessionId
        {
            get
            {
                if (_sessionId == Guid.Empty && LayoutParameter.ParameterValue is not null && LayoutParameter.ParameterValue.SessionId != Guid.Empty)
                    _sessionId = LayoutParameter.ParameterValue.SessionId;
                return _sessionId;
            }
            set => _sessionId = value;
        }

        public bool IsCached { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public BlazorParameter<ComponentState> InteractionState
        {
            get
            {
                return _bpState ??= new BlazorParameter<ComponentState>(null);
            }
            set
            {

                if (value?.ParameterValue?.IsForced == true && _bpState?.HasValue() == true && _bpState.ParameterValue != value.ParameterValue)
                    throw new Exception("State is forced and it cannot be changed");
                _bpState = value;
            }
        }

        [Parameter]
        public BlazorParameter<bool?> InheritState { get; set; }

        [Parameter]
        public BlazorParameter<bool?> DisabledByDefault { get; set; }

        [Parameter(CaptureUnmatchedValues = true)]
        public Dictionary<string, object> AdditionalAttributes { get; set; } = new();

        [CascadingParameter]
        public BlazorParameter<MyEditContext> CascadedEditContext { get; set; }

        [CascadingParameter(Name = "LayoutParameter")]
        public BlazorParameter<MyLayoutComponentBase> LayoutParameter { get; set; }

        [CascadingParameter(Name = "ParentParameter")]
        public BlazorParameter<MyComponentBase> ParentParameter { get; set; }

        public MyLayoutComponentBase Layout => LayoutParameter?.ParameterValue;
        public MyComponentBase Parent => ParentParameter?.ParameterValue;
        public List<MyComponentBase> Children
        {
            get
            {
                //_syncComponentsCache.Wait();
                var children = Layout.Components.SafelyGetValues().Where(c => c.Parent == this && !c.IsDisposed).ToList();
                //_syncComponentsCache.Release();
                return children;
            }
        }

        public List<MyComponentBase> ChildrenAndSelf => Children.Prepend_(this).ToList();

        public List<MyComponentBase> Descendants
        {
            get
            {
                var descendants = Children;
                foreach (var child in Children)
                    descendants.AddRange(child.Descendants);
                return descendants;
            }
        }

        public List<MyComponentBase> Ancestors
        {
            get
            {
                var ancestors = new List<MyComponentBase>();
                var parent = Parent;
                while (parent is not null)
                {
                    ancestors.Add(parent);
                    parent = ancestors.Last().Parent;
                }
                //if (Parent is not null)
                //{
                //    ancestors.Add(Parent);
                //    ancestors.AddRange(Parent.Ancestors);
                //    if (ancestors.Count > 10)
                //    {
                //        var t = 0;
                //    }
                //}

                return ancestors;
            }
        }

        public List<MyComponentBase> Siblings => Parent is not null ? Parent.Children.Except(this).ToList() : Layout.Components.Values.Where(c => !c.IsLayout && !c.IsCommonLayout && c.Parent is null && c != this).ToList();

        public AuthenticateUserVM AuthenticatedUser
        {
            get => !IsCommonLayout ? Layout.AuthenticatedUser : _authenticatedUser;
            set
            {
                var authUser = (!IsCommonLayout ? Layout.AuthenticatedUser : _authenticatedUser) ?? AuthenticateUserVM.NotAuthenticated;
                authUser = Mapper.Map(value, authUser);
                if (!IsCommonLayout)
                    Layout.AuthenticatedUser = authUser;
                else
                    _authenticatedUser = authUser;
            }
        }

        public MyNavBarBase NavBar => Layout.Components.Values.OfType<MyNavBarBase>().Single();

        public bool AdditionalAttributesHaveChanged { get; private set; }

        public bool PreventRender { get; set; }

        public virtual bool IsAuthorized => false;

        [Inject]
        public HttpClient HttpClient { get; set; }

        [Inject]
        public NavigationManager NavigationManager { get; set; }

        [Inject]
        public IConfiguration Configuration { get; set; }

        [Inject]
        public IJSRuntime JsRuntime { get; set; }

        [Inject]
        public IMyJsRuntime MyJsRuntime { get; set; }

        [Inject]
        public IAccountClient AccountClient { get; set; }

        [Inject]
        public IAdminClient AdminClient { get; set; }

        [Inject]
        public IBackendInfoClient BackendInfoClient { get; set; }

        [Inject]
        public AuthenticationStateProvider AuthStateProvider { get; set; }
        public UserAuthenticationStateProvider UserAuthStateProvider => (UserAuthenticationStateProvider)AuthStateProvider;

        [Inject]
        public IParametersCacheService ParametersCache { get; set; }

        [Inject]
        public ISessionStorageService SessionStorage { get; set; }

        //[Inject]
        //public ISessionCacheService SessionCache { get; set; }

        [Inject]
        public IHttpContextAccessor HttpContextAccessor { get; set; }

        [Inject]
        public IRequestScopedCacheService RequestScopedCache { get; set; }

        [Inject]
        public IMapper Mapper { get; set; }

        protected MyComponentBase()
        {
            _renderFragment = builder =>
            {
                _hasPendingQueuedRender = false;
                BuildRenderTree(builder);
                //OnStateChangedAsync += MyComponentBase_StateChangedAsync;
            };
        }

        public async Task<MyPromptBase> GetPromptAsync() => _prompt ??= await ComponentByTypeAsync<MyPromptBase>();

        protected virtual void BuildRenderTree(RenderTreeBuilder builder) { } // code within this class should *not* invoke BuildRenderTree directly, use `_renderFragment` instead

        public Task HandleEventAsync(EventCallbackWorkItem callback, object arg) => callback.InvokeAsync(arg);

        public virtual async Task SetParametersAsync(ParameterView parameters)
        {
            if (IsDisposed || JsRuntime == null)
                return;

            try
            {
                parameters.SetParameterProperties(this);
                SetNullParametersToDefaults(); // due to this, init all params with `=` and not `??=` (or use HasValue())

                await InitializeAsync();
                await SetParametersAsync(true);
                await StateHasChangedAsync(true);
            }
            catch (Exception ex) when (ex is TaskCanceledException or ObjectDisposedException)
            {
                //Logger.For<MyComponentBase>().Warn("'SetParametersAsync' was canceled, disposed component?");
            }
            catch (Exception)
            {
                if (!IsDisposed)
                    throw;
            }
        }

        private async Task InitializeAsync()
        {
            if (!_isInitialized)
            {
                if (_guid == Guid.Empty) // OnInitializedAsync runs twice by default, once for pre-render and once for the actual render | fixed by using IComponent interface directly
                    _guid = Guid.NewGuid();
                _bpParentToCascade = new BlazorParameter<MyComponentBase>(this);
                if (IsCommonLayout) // set LayoutComponentBase_Layout as generic layout
                {
                    var thisAsLayout = (MyLayoutComponentBase)this;
                    thisAsLayout._bpLayoutToCascade = new BlazorParameter<MyLayoutComponentBase>(thisAsLayout);
                }

                OnInitialized();
                await OnInitializedAsync();

                _isInitialized = true;
                _firstRenderAfterInit = true;
                _firstParamSetup = true;
            }
        }

        protected virtual void OnInitialized() { }
        protected virtual async Task OnInitializedAsync() => await Task.CompletedTask;

        private async Task SetParametersAsync(bool forceSetCascadingParamsAsChangedOnFirstSetup)
        {
            // Set Parameters
            if (_firstParamSetup)
            {
                if (forceSetCascadingParamsAsChangedOnFirstSetup)
                    SetCascadingBlazorParametersAsChanged();

                if (LayoutParameter.HasValue() && !IsDisposed) // Style components would not have Layout value as they are rendered manually to a css file so we need `Layout.HasValue()`, also a specialized layout from an app would have a value and wouldn't be a common layout so I need to account for that lateer, here `&& !_isCommonLayout && !_isSpecialisedLayout` is not needed because MyLayoutComponent utilizes these event | IsDisposed is an edge case, I don't want the component to remain in cache if it was disposed before params were initialised
                {
                    Layout.Components[_guid] = this;
                    IsCached = true;
                    Layout.LayoutSessionIdSet -= Layout_SessionIdSet; // also add an event to layout itself as well so app can trigger Rebuild component cache
                    Layout.LayoutSessionIdSet += Layout_SessionIdSet;
                    Layout.DeviceSizeChanged -= Layout_DeviceSizeChanged;
                    Layout.DeviceSizeChanged += Layout_DeviceSizeChanged;
                }

                await OnFirstParametersSetAsync();
            }

            AdditionalAttributesHaveChanged = !AdditionalAttributes.Keys.CollectionEqual(_prevAdditionalAttributes.Keys) || !AdditionalAttributes.Values.CollectionEqual(_prevAdditionalAttributes.Values);
            _prevAdditionalAttributes = AdditionalAttributes.ToOrderedDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());

            OnParametersSet();
            await OnParametersSetAsync();

            if (InheritState.HasChanged())
                InheritState.ParameterValue ??= InheritState.V ?? true;

            if (DisabledByDefault.HasChanged())
                DisabledByDefault.ParameterValue ??= DisabledByDefault.V ?? true;

            var parentState = InheritState.V == true ? Ancestors.FirstOrNull(a => a.InteractionState.HasChanged())?.InteractionState?.V : null;
            var anyParentIsEnabledByDefault = InheritState.V == true && Ancestors.Any(a => a.DisabledByDefault.V == false);
            //Logger.For(GetType()).Info($"{GetType().Name}: Setting params - state changed: c: {(InteractionState.HasChanged() ? "true" : "false")}, p: {(parentState is not null ? "true" : "false")} | state: c: {InteractionState.V}, P: {parentState}");
            
            if (InteractionState.HasChanged() || parentState is not null)
            {
                ComponentState thisAsIconState = null;
                if (this is MyIconBase)
                    thisAsIconState = (Ancestors.FirstOrNull(a => a is MyButtonBase or MyInputBase or MyDropDownBase) ?? Ancestors.FirstOrNull(a => a is MyInputGroupBase))?.InteractionState.V;

                InteractionState.ParameterValue = thisAsIconState ?? parentState ?? InteractionState.V.NullifyIf(_ => !InteractionState.HasChanged()) ?? (DisabledByDefault.V == true && !anyParentIsEnabledByDefault ? ComponentState.Disabled : ComponentState.Enabled);
                InteractionState.SetAsUnchanged();
                
                if (this is MyIconBase icon2 && icon2.IconType.V.LightIcon?.EnumToString().Contains("Dollar") == true)
                {
                    Logger.For<MyIconBase>().Info($"PARAMS: {this}, {icon2.IconType.V.LightIcon.EnumToString()}: {InteractionState.V.State.EnumToString()}");
                }

                if (InteractionState.ParameterValue.IsDisabledOrForceDisabled)
                {
                    RemoveClasses("my-loading");
                    AddAttribute("disabled", string.Empty);
                    AddClass("disabled");
                }
                else if (InteractionState.ParameterValue.IsLoadingOrForceLoading)
                {
                    RemoveClasses("disabled");
                    RemoveAttribute("disabled");
                    AddClass("my-loading");
                }
                else
                {
                    RemoveClasses("my-loading");
                    RemoveAttribute("disabled");
                    RemoveClass("disabled");
                }
            }

            _firstParamSetup = false;
            SetAllBlazorParametersAsUnchanged();
        }

        protected virtual async Task OnFirstParametersSetAsync() => await Task.CompletedTask;
        protected virtual void OnParametersSet() { }
        protected virtual async Task OnParametersSetAsync() => await Task.CompletedTask;

        async Task IHandleAfterRender.OnAfterRenderAsync()
        {
            if (IsDisposed || JsRuntime is null)
                return;

            try
            {
                await _syncRender.WaitAsync(); // if `State` is being changed manually by calling `StateHasChangedAsync` also block render | For instance first render may enter this method and subsequent render can enter as well before the first render finished thus leaving some parts like session not initialized properly

                if (_firstRenderAfterInit)
                {
                    if (IsCommonLayout)
                    {
                        await SetSessionIdAsync();
                        //SessionCache.AddIfNotExistsAndGet(SessionId, new SessionCacheData()).CurrentLayout = (MyLayoutComponentBase)this;
                        await PromptModuleAsync; // this makes prompt js available within any component
                        var prompts = await ComponentsByTypeAsync<MyPromptBase>();
                        foreach (var prompt in prompts)
                            await prompt.StateHasChangedAsync();

                        var thisAsLayout = (MyLayoutComponentBase)this;
                        var mediaQueryDotNetRef = DotNetObjectReference.Create(thisAsLayout);
                        thisAsLayout.DeviceSize = (await (await thisAsLayout.MediaQueryModuleAsync).InvokeAndCatchCancellationAsync<string>("blazor_MediaQuery_SetupForAllDevicesAndGetDeviceSizeAsync", StylesConfig.DeviceSizeKindNamesWithMediaQueries, _guid, mediaQueryDotNetRef)).ToEnum<DeviceSizeKind>();

                        await SessionStorage.SetItemAsStringAsync("BackendBaseUrl", ConfigUtils.BackendBaseUrl);

                        var navBar = await ComponentByTypeAsync<MyNavBarBase>();
                        await navBar.Setup();

                        await ComponentBaseModuleAsync; // needed i.e.: for handling events of non-nativee components

                        await thisAsLayout.OnLayoutSessionIdSettingAsync(SessionId);
                    }

                    await OnAfterFirstRenderAsync();
                }

                if (IsDisposed)
                    return;

                if (IsAuthorized && _isFirstRenderAfterAuthorization)
                {
                    _isFirstRenderAfterAuthorization = false;
                    await OnAfterFirstRenderAfterAutthorizationAsync();
                }
                OnAfterRender(_firstRenderAfterInit);
                await OnAfterRenderAsync(_firstRenderAfterInit);
                await OnAfterRenderFinishingAsync(_firstRenderAfterInit);

                //if (LayoutParameter.HasValue() && SessionId != Guid.Empty && Layout.DeviceSize is not null && isFirstRenderAfterInit) // it means component was loaded some time after layout which means layout couldn't trigger the event for it because it wasn't aavailable at the time
                if (_firstRenderAfterInit && Layout?.IsRendered == true)
                    await Layout_SessionIdSet(null, new MyLayoutComponentBase.LayoutSessionIdSetEventArgs(SessionId), CancellationToken.None);

                IsRerendered = true;
                //Logger.For<MyComponentBase>().Info($"{GetType().Name}: Rerendered");
                _firstRenderAfterInit = false;
            }
            catch (Exception ex) when (ex is TaskCanceledException or ObjectDisposedException or JSDisconnectedException)
            {
                //Logger.For<MyComponentBase>().Warn("'OnAfterRenderAsync' was canceled, disposed component?");
            }
            catch (Exception ex)
            {
                if (!IsDisposed)
                    throw;
            }
            finally
            {
                if (!IsDisposed && _syncRender.CurrentCount == 0) // Release render if we are changing `State` manually so `StateHasChanged` knows about it
                    await _syncRender.ReleaseAsync();
            }
        }

        private async Task Layout_SessionIdSet(MyComponentBase sender, MyLayoutComponentBase.LayoutSessionIdSetEventArgs e, CancellationToken token)
        {
            await _syncAfterSessionIdSet.WaitAsync();

            if (IsDisposed)
                return; // semaphore is disposed on component dispose
            if (IsCommonLayout || Layout is null || _sessionIdAlreadySet)
            {
                await _syncAfterSessionIdSet.ReleaseAsync();
                return;
            }

            _sessionIdAlreadySet = true;
            await OnLayoutAfterRenderFinishedAsync(e.Sessionid, Layout.DeviceSize ?? throw new NullReferenceException("Device Size shouldn't be null"));

            await _syncAfterSessionIdSet.ReleaseAsync();
        }

        protected virtual async Task OnAfterFirstRenderAsync() => await Task.CompletedTask;
        protected virtual void OnAfterRender(bool firstRender) { }
        protected virtual async Task OnAfterRenderAsync(bool firstRender) => await Task.CompletedTask;
        protected virtual async Task OnAfterFirstRenderAfterAutthorizationAsync() => await Task.CompletedTask;

        protected virtual async Task OnLayoutSessionIdSetAsync() => await Task.CompletedTask;
        protected virtual async Task OnLayoutAfterRenderFinishedAsync(Guid sessionId, DeviceSizeKind deviceSize) => await Task.CompletedTask;

        private async Task Layout_DeviceSizeChanged(MyLayoutComponentBase sender, MyMediaQueryChangedEventArgs e, CancellationToken token)
        {
            await OnDeviceSizeChangedAsync(e.DeviceSize);
        }

        protected virtual async Task OnDeviceSizeChangedAsync(DeviceSizeKind deviceSize) => await Task.CompletedTask;

        protected Task InvokeAsync(Action workItem) => _renderHandle.Dispatcher.InvokeAsync(workItem);

        protected Task InvokeAsync(Func<Task> workItem) => _renderHandle.Dispatcher.InvokeAsync(workItem);

        protected bool IsFirstParamSetup() => _firstParamSetup;

        protected bool HasAuthenticationStatus(AuthStatus authStatus) => AuthenticatedUser == null && authStatus == AuthStatus.NotChecked || AuthenticatedUser != null && AuthenticatedUser.HasAuthenticationStatus(authStatus);

        protected bool HasAnyAuthenticationStatus(params AuthStatus[] authStatuses) => AuthenticatedUser == null && AuthStatus.NotChecked.In(authStatuses) || AuthenticatedUser != null && AuthenticatedUser.HasAnyAuthenticationStatus(authStatuses);

        protected async Task<ComponentAuthenticationStatus> AuthenticateAsync(bool changeStateEvenIfAuthUserIsTheSame)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().Name);
            var navBar = await ComponentByTypeAsync<MyNavBarBase>();
            var authResponse = await AccountClient.GetAuthenticatedUserAsync();
            var prevAuthUser = Mapper.Map(AuthenticatedUser, new AuthenticateUserVM());
            //Logger.For(GetType()).Info($"Setting Auth user to {authResponse.Result}");

            var authPerformed = !authResponse.IsError;
            var authChanged = !authResponse.IsError && !authResponse.Result.Equals(prevAuthUser);
            var authSuccessful = !authResponse.IsError && authResponse.Result.HasAuthenticationStatus(AuthStatus.Authenticated);

            var authStatus = new ComponentAuthenticationStatus
            {
                AuthenticationPerformed = authPerformed,
                AuthenticationChanged = authChanged,
                AuthenticationSuccessful = authSuccessful,
                ResponseMessage = authResponse.IsError ? authResponse.Message : null
            };

            if (!authResponse.IsError && !authResponse.Result.Equals(prevAuthUser) || changeStateEvenIfAuthUserIsTheSame)
            {
                AuthenticatedUser = authResponse.Result; // ta the end because AUthenticatedUser servees as a Parameter in Login.razor so I don't want to cause rerendeer and changing the valuee prematurely
                await StateHasChangedAsync(true);
                await navBar.StateHasChangedAsync(true);
                var page = navBar.Siblings.SingleOrNull(c => c.IsPage);
                if (page is not null && page != this) // it would be null for a subpage that doesn't definee any 'Page' component, i.e.: empty page
                    await page.StateHasChangedAsync(true);
                //await Layout.Components.Values.Single(c => c._isLayout).StateHasChangedAsync(true);
                //if (!changeStateEvenIfAuthUserIsTheSame)
                //    Logger.For(GetType()).Info("Auth User changed, updating this control and navbar state");
                //else
                //    Logger.For(GetType()).Info("Auth User didn't change but state will be force changed, updating this control and navbar state");
            }
            //else
            //    Logger.For(GetType()).Info("Auth User didn't change, doing nothing");

            return authStatus;
        }

        protected async Task<bool> EnsureAuthenticatedAsync(bool displayErrorMessage, bool changeStateEvenIfAuthUserIsTheSame) // true if user authenticated
        {
            var authStatus = await AuthenticateAsync(changeStateEvenIfAuthUserIsTheSame);
            if ((authStatus.AuthenticationFailed || authStatus.ResponseMessage is not null) && displayErrorMessage)
                await PromptMessageAsync(NotificationType.Error, authStatus.ResponseMessage ?? "You are not Authenticated");
            return authStatus.AuthenticationSuccessful;
        }

        protected async Task<bool> EnsureAuthenticationPerformedAsync(bool displayErrorMessage, bool changeStateEvenIfAuthUserIsTheSame) // true if authentication didn't throw, regardless if user is authenticated
        {
            var authStatus = await AuthenticateAsync(changeStateEvenIfAuthUserIsTheSame);
            if ((authStatus.AuthenticationNotPerformed || authStatus.ResponseMessage is not null) && displayErrorMessage)
                await PromptMessageAsync(NotificationType.Error, authStatus.ResponseMessage ?? "Authentication was not performed");
            return authStatus.AuthenticationPerformed;
        }

        protected async Task<bool> EnsureAuthenticationChangedAsync(bool displayErrorMessage, bool changeStateEvenIfAuthUserIsTheSame) // true if authentication state changed, regardless if user is authenticated
        {
            var authStatus = await AuthenticateAsync(changeStateEvenIfAuthUserIsTheSame);
            if ((authStatus.AuthenticationNotChanged || authStatus.ResponseMessage is not null) && displayErrorMessage)
                await PromptMessageAsync(NotificationType.Error, authStatus.ResponseMessage ?? "Authentication State didn't change");
            return authStatus.AuthenticationChanged;
        }

        protected void SetMainCustomAndUserDefinedClasses(string mainClass, IEnumerable<string> customClasses, bool preserveExistingClasses = false)
        {
            if (IsDisposed)
                return;

            _syncClasses.Wait();

            if (!preserveExistingClasses)
                Classes.Clear();
            if (!mainClass.IsNullOrWhiteSpace())
                Classes.Add(mainClass);
            if (customClasses != null)
                Classes.AddRange(customClasses.Where(c => !c.IsNullOrWhiteSpace()));
            var additionalClasses = AdditionalAttributes.VorN("class")?.ToString().NullifyIf(s => s.IsNullOrWhiteSpace())?.Split(" ");
            if (additionalClasses != null)
                Classes.AddRange(additionalClasses.Where(c => !c.IsNullOrWhiteSpace()));
            Classes.ReplaceAll(Classes.Distinct());
            _renderClasses = Classes.JoinAsString(" ");

            _syncClasses.Release();
        }

        protected void SetMainAndUserDefinedClasses(string mainClass, bool preserveExistingClasses = false) => SetMainCustomAndUserDefinedClasses(mainClass, null, preserveExistingClasses);
        protected void SetUserDefinedClasses(bool preserveExistingClasses = false) => SetMainCustomAndUserDefinedClasses(null, null, preserveExistingClasses);

        protected MyComponentBase AddClasses(string cls, params string[] classes)
        {
            if (IsDisposed)
                return this;

            _syncClasses.Wait();

            if (classes != null)
                Classes.AddRange(classes.Prepend_(cls).Where(c => !c.IsNullOrWhiteSpace() && !c.In(Classes)));
            Classes.ReplaceAll(Classes.Distinct());
            _renderClasses = Classes.JoinAsString(" ");
            _syncClasses.Release();

            return this;
        }

        public MyComponentBase AddClasses(IEnumerable<string> classes)
        {
            if (IsDisposed)
                return this;

            _syncClasses.Wait();

            if (classes != null)
                Classes.AddRange(classes.Where(c => !c.IsNullOrWhiteSpace()));
            Classes.ReplaceAll(Classes.Distinct());
            _renderClasses = Classes.JoinAsString(" ");

            _syncClasses.Release();

            return this;
        }

        public MyComponentBase AddClass(string cls) => AddClasses(cls);

        public MyComponentBase RemoveClasses(string cls, params string[] classes)
        {
            return RemoveClasses(classes.Prepend_(cls).ToArray());
        }

        public MyComponentBase RemoveClasses(string[] classes)
        {
            if (IsDisposed)
                return this;

            _syncClasses.Wait();

            Classes.RemoveAll(s => s.In(classes));
            Classes.ReplaceAll(Classes.Distinct());
            _renderClasses = Classes.JoinAsString(" ");

            _syncClasses.Release();

            return this;
        }

        public MyComponentBase RemoveClass(string cls) => RemoveClasses(cls);

        protected void SetCustomAndUserDefinedStyles(Dictionary<string, string> customStyles, bool preserveExistingStyles = false)
        {
            if (IsDisposed)
                return;

            _syncStyles.Wait();

            if (!preserveExistingStyles)
                _style.Clear();
            if (customStyles != null)
                _style.AddRange(customStyles.Where(s => !s.Value.IsNullOrWhiteSpace() && !s.Key.In(_style.Keys)));
            var userDefinedStyles = AdditionalAttributes.VorN("style")?.ToString().NullifyIf(s => s.IsNullOrWhiteSpace())?.CssStringToDictionary();
            if (userDefinedStyles != null)
                _style.AddRange(userDefinedStyles.Where(s => !s.Value.IsNullOrWhiteSpace() && !s.Key.In(_style.Keys)));
            _renderStyle = _style.CssDictionaryToString();

            _syncStyles.Release();
        }

        protected void SetCustomStyles(Dictionary<string, string> customStyles, bool preserveExistingStyles = false)
        {
            if (IsDisposed)
                return;

            _syncStyles.Wait();

            if (!preserveExistingStyles)
                _style.Clear();
            if (customStyles != null)
                _style.AddRange(customStyles.Where(s => !s.Value.IsNullOrWhiteSpace() && !s.Key.In(_style.Keys)));
            _renderStyle = _style.CssDictionaryToString();

            _syncStyles.Release();
        }

        protected void SetCustomStyles(Dictionary<string, string>[] customStyleDicts, bool preserveExistingStyles = false)
        {
            if (IsDisposed)
                return;

            _syncStyles.Wait();

            if (!preserveExistingStyles)
                _style.Clear();
            foreach (var styles in customStyleDicts)
                _style.AddRange(styles.Where(s => !s.Value.IsNullOrWhiteSpace() && !s.Key.In(_style.Keys)));
            _renderStyle = _style.CssDictionaryToString();

            _syncStyles.Release();
        }

        protected Dictionary<string, string> GetUserDefinedStyles()
        {
            return AdditionalAttributes.VorN("style")?.ToString().NullifyIf(s => s.IsNullOrWhiteSpace())?.CssStringToDictionary() ?? new Dictionary<string, string>();
        }

        protected void SetUserDefinedStyles(bool preserveExistingStyles = false) => SetCustomAndUserDefinedStyles(null, preserveExistingStyles);

        protected void AddStylesIfNotExist(Dictionary<string, string> customStyles)
        {
            if (IsDisposed)
                return;

            _syncStyles.Wait();

            if (customStyles != null)
                _style.AddRange(customStyles.Where(s => !s.Value.IsNullOrWhiteSpace() && !s.Key.In(_style.Keys)));
            _renderStyle = _style.CssDictionaryToString();

            _syncStyles.Release();
        }

        protected void AddStyleIfNotExist(string key, string value) => AddStylesIfNotExist(new Dictionary<string, string> { [key] = value });

        protected void AddStyles(Dictionary<string, string> customStyles)
        {
            if (IsDisposed)
                return;

            _syncStyles.Wait();

            if (customStyles != null)
                foreach (var (key, value) in customStyles)
                    if (!value.IsNullOrWhiteSpace())
                        _style.AddOrUpdate(key, value);

            _renderStyle = _style.CssDictionaryToString();

            _syncStyles.Release();
        }

        protected void AddStyle(string key, string value) => AddStyles(new Dictionary<string, string> { [key] = value });

        protected void RemoveStyles(string[] customStyles)
        {
            if (IsDisposed)
                return;

            _syncStyles.Wait();

            if (customStyles != null)
                foreach (var key in customStyles)
                    _style.RemoveIfExists(key);

            _renderStyle = _style.CssDictionaryToString();

            _syncStyles.Release();
        }

        protected void RemoveStyle(string key) => RemoveStyles(new[] { key });

        protected void SetCustomAndUserDefinedAttributes(Dictionary<string, string> customAttributes, bool preserveExistingAttributes = false)
        {
            if (IsDisposed)
                return;

            _syncAttributes.Wait();

            if (!preserveExistingAttributes)
                _attributes.Clear();
            if (customAttributes != null)
                _attributes.AddRange(customAttributes.Where(a => !a.Value.IsNullOrWhiteSpace() && !a.Key.In(_attributes.Keys)));
            var userDefinedAttributes = AdditionalAttributes?.Where(kvp => !kvp.Key.In("class", "style"));
            if (userDefinedAttributes != null)
                _attributes.AddRange(userDefinedAttributes
                    .Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value?.ToString()))
                    .Where(kvp => !kvp.Key.In("class", "style"))
                    .Where(kvp => (customAttributes == null || !kvp.Key.In(customAttributes.Keys)) && !kvp.Key.In(_attributes.Keys)));
            _renderAttributes.ReplaceAll(_attributes.ValuesToObjects());
            _id = AdditionalAttributes.VorN("id")?.ToString().NullifyIf(s => s.IsNullOrWhiteSpace());

            _syncAttributes.Release();
        }

        protected void SetUserDefinedAttributes(bool preserveExistingAttributes = false) => SetCustomAndUserDefinedAttributes(null, preserveExistingAttributes);

        protected void AddAttributes(Dictionary<string, string> attributes)
        {
            if (IsDisposed)
                return;

            _syncAttributes.Wait();

            foreach (var (key, value) in attributes.Where(attr => !attr.Key.In("class", "style")))
                _attributes[key] = value;
            _renderAttributes.ReplaceAll(_attributes.ValuesToObjects());

            _syncAttributes.Release();
        }

        protected void AddAttribute(string key, string value) => AddAttributes(new Dictionary<string, string> { [key] = value });

        protected void RemoveAttributes(string[] attributeKeys)
        {
            if (IsDisposed)
                return;

            _syncAttributes.Wait();

            foreach (var key in attributeKeys)
                _attributes.Remove(key);
            _renderAttributes.ReplaceAll(_attributes.ValuesToObjects());

            _syncAttributes.Release();
        }

        protected void RemoveAttribute(string key) => RemoveAttributes(new[] { key });

        private PropertyInfo[] GetAllBlazorParamPropertyInfos()
        {
            return GetType().GetProperties().Where(prop =>
                (prop.IsDefined(typeof(ParameterAttribute), false) || prop.IsDefined(typeof(CascadingParameterAttribute), false))
                && prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(BlazorParameter<>)).ToArray();
        }

        private void SetNullParametersToDefaults()
        {
            var blazorParameters = GetAllBlazorParamPropertyInfos();
            foreach (var bp in blazorParameters)
                if (bp.GetValue(this) == null)
                    bp.SetValue(this, Activator.CreateInstance(typeof(BlazorParameter<>).MakeGenericType(bp.PropertyType.GetGenericArguments()), new object[] { null }));
        }

        private void SetAllBlazorParametersAsUnchanged()
        {
            var blazorParameters = GetAllBlazorParamPropertyInfos();
            foreach (var bp in blazorParameters)
                bp.PropertyType.GetMethod("SetAsUnchanged")?.Invoke(bp.GetValue(this), null);
        }

        private void SetCascadingBlazorParametersAsChanged()
        {
            var blazorCascadingParameters = GetAllBlazorParamPropertyInfos().Where(pi => pi.IsDefined(typeof(CascadingParameterAttribute), false)).ToArray();
            foreach (var cbp in blazorCascadingParameters)
                cbp.PropertyType.GetMethod("SetAsChanged")?.Invoke(cbp.GetValue(this), null);
        }

        protected void StateHasChanged(bool force)
        {
            if (_hasPendingQueuedRender)
                return;

            if ((!IsRendered || _renderHandle.IsRenderingOnMetadataUpdate || force) && !PreventRender)
            {
                _hasPendingQueuedRender = true;

                try
                {
                    _renderHandle.Render(_renderFragment);
                }
                catch
                {
                    _hasPendingQueuedRender = false;
                    throw;
                }
            }

            if (_preventRenderOnce && PreventRender)
            {
                _preventRenderOnce = false;
                PreventRender = false;
            }
        }

        public async Task<MyComponentBase> StateHasChangedAsync(bool force = false, bool waitForRerender = false)
        {
            if (waitForRerender)
                ClearControlRerenderingStatus();

            await InvokeAsync(() => StateHasChanged(force));
            if (waitForRerender)
                await WaitForControlToRerenderAsync();
            return this;
        }

        public void PreventNextRender()
        {
            PreventRender = true;
            _preventRenderOnce = true;
        }

        //public async Task<MyComponentBase> WaitForRenderAsync()
        //{
        //    await _syncStateChanged.WaitAsync();
        //    //await TaskUtils.WaitUntil(() => _syncRender.CurrentCount == 0); // wait for aquiring lock in `AfterRender`
        //    await TaskUtils.WaitUntil(() => _syncStateChanged.CurrentCount == 1); // wait for releasing `StateChanged` lock which merging `StateChanged` with `AfterRender`
        //    await TaskUtils.WaitUntil(() => _syncRender.CurrentCount == 1); // wait for entire `AfterRender` to complete

        //    return this;
        //}

        private event MyAsyncEventHandler<MyComponentBase, EventArgs> OnStateChangedAsync;
        private async Task OnStateChangingAsync() => await OnStateChangedAsync.InvokeAsync(this, EventArgs.Empty);

        public async Task<MyComponentBase> NotifyParametersChangedAsync(bool forceSetCascadingParamsAsChangedOnFirstSetup = true)
        {
            await SetParametersAsync(forceSetCascadingParamsAsChangedOnFirstSetup); // for when params were not really changed but their values were
            return this;
        }

        public void Attach(RenderHandle renderHandle)
        {
            if (_renderHandle.IsInitialized)
                throw new InvalidOperationException($"The render handle is already set. Cannot initialize a {nameof(ComponentBase)} more than once.");

            _renderHandle = renderHandle;
        }

        protected async Task PromptMessageAsync(NotificationType status, string message) // don't refresh if called on initialized
        {
            await (await GetPromptAsync()).AddNotificationAsync(status, message);
        }

        private async Task SetSessionIdAsync()
        {
            if (SessionId == Guid.Empty)
                SessionId = await MyJsRuntime.GetOrCreateSessionIdAsync(); // await SessionStorage.GetOrCreateSessionIdAsync();
        }

        public async Task<Guid> GetSessionIdAsync()
        {
            if (SessionId == Guid.Empty)
                SessionId = await MyJsRuntime.GetSessionIdAsync();

            return SessionId;
        }

        public async Task<Guid> GetSessionIdOrEmptyAsync()
        {
            if (SessionId == Guid.Empty)
            {
                var sessionid = await MyJsRuntime.GetSessionIdOrEmptyAsync();
                if (sessionid != Guid.Empty)
                    SessionId = sessionid;
            }

            return SessionId;
        }

        public async Task<Guid> GetTemporarySessionIdAsync() // for use when JsInterop is not available i.e.: `OnInitialized`, `OnParametersSet` 
        {
            if (RequestScopedCache.TemporarySessionId == Guid.Empty)
            {
                RequestScopedCache.TemporarySessionId = Guid.NewGuid();
                //Logger.For<MyComponentBase>().Info($"Creating new teemporary session: {RequestScopedCache.TemporarySessionId}");
            }

            return await Task.FromResult(RequestScopedCache.TemporarySessionId);
        }

        public async Task<List<TComponent>> ComponentsByClassAsync<TComponent>(string cssClass) where TComponent : MyComponentBase
        {
            //await _syncComponentsCache.WaitAsync();
            var componentsByClass = (Layout ?? (MyLayoutComponentBase)this).Components.SafelyGetValues().OfType<TComponent>().Where(c => cssClass.In(c.Classes)).ToList();
            //await _syncComponentsCache.ReleaseAsync();
            return await Task.FromResult(componentsByClass);
        }

        public async Task<TComponent> ComponentByClassAsync<TComponent>(string cssClass) where TComponent : MyComponentBase
        {
            return (await ComponentsByClassAsync<TComponent>(cssClass)).Single();
        }

        public async Task<TComponent> ComponentByIdAsync<TComponent>(string id) where TComponent : MyComponentBase
        {
            //await _syncComponentsCache.WaitAsync();
            var componentById = (Layout ?? (MyLayoutComponentBase)this).Components.SafelyGetValues().ToArray().OfType<TComponent>().Single(c => id.EqualsInvariant(c._id));
            //await _syncComponentsCache.ReleaseAsync();
            return await Task.FromResult(componentById);
        }

        public async Task<TComponent> ComponentByGuidAsync<TComponent>(Guid guid) where TComponent : MyComponentBase
        {
            //await _syncComponentsCache.WaitAsync();
            var componentByGuid = (Layout ?? (MyLayoutComponentBase)this).Components.SafelyGetValues().ToArray().OfType<TComponent>().Single(c => guid.Equals(c._guid));
            //await _syncComponentsCache.ReleaseAsync();
            return await Task.FromResult(componentByGuid);
        }

        public async Task<List<TComponent>> ComponentsByTypeAsync<TComponent>() where TComponent : MyComponentBase
        {
            //await _syncComponentsCache.WaitAsync();
            var componentsByType = (Layout ?? (MyLayoutComponentBase)this).Components.SafelyGetValues().ToArray().OfType<TComponent>().ToList();
            //await _syncComponentsCache.ReleaseAsync();
            return await Task.FromResult(componentsByType);
        }

        public async Task<TComponent> ComponentByTypeAsync<TComponent>() where TComponent : MyComponentBase
        {
            return (await ComponentsByTypeAsync<TComponent>()).Single();
        }

        public async Task ShowLoginModalAsync() => await ComponentByClassAsync<MyModalBase>("my-login-modal").ShowModalAsync();
        public async Task HideLoginModalAsync() => await ComponentByClassAsync<MyModalBase>("my-login-modal").HideModalAsync();

        protected static void ClearControlsRerenderingStatus(IEnumerable<MyComponentBase> controls) => controls.ForEach(c => c.IsRerendered = false);

        protected static void ClearControlRerenderingStatus(MyComponentBase control) => ClearControlsRerenderingStatus(new[] { control });

        protected void ClearControlRerenderingStatus() => ClearControlRerenderingStatus(this);

        protected static async Task WaitForControlsToRerenderAsync(IEnumerable<MyComponentBase> controls)
        {
            var arrControls = controls.ToArray();
            var wereRerenderedAtSomePoint = new List<MyComponentBase>();
            await TaskUtils.WaitUntil(() =>
            {
                foreach (var c in arrControls)
                    if (c.IsRerendered && !c.In(wereRerenderedAtSomePoint))
                        wereRerenderedAtSomePoint.Add(c);

                return wereRerenderedAtSomePoint.Count == arrControls.Length || arrControls.All(c => c.InteractionState.V.IsForced) || arrControls.Any(c => c.IsDisposed);
            }, 25, 10000);
            ClearControlsRerenderingStatus(arrControls);
        }

        protected static Task WaitForControlToRerenderAsync(MyComponentBase control) => WaitForControlsToRerenderAsync(new[] { control });

        protected Task WaitForControlToRerenderAsync() => WaitForControlToRerenderAsync(this);

        //protected async Task SetControlStatesAsync(ButtonState state, IEnumerable<MyComponentBase> controlsToChangeState, MyButtonBase btnLoading = null, bool changeRenderingState = true) => await SetControlStatesAsync(state.ToComponentState().State ?? throw new NullReferenceException(), controlsToChangeState, btnLoading, changeRenderingState);

        public async Task SetControlStatesAsync(ComponentState state, IEnumerable<MyComponentBase> controlsToChangeState, MyComponentBase componentLoading = null, ChangeRenderingStateMode changeRenderingState = ChangeRenderingStateMode.AllSpecified, IEnumerable<MyComponentBase> controlsToAlsoChangeRenderingState = null)
        { // including Current should generally fail during AfterRender because after rendering happens inside sempahore
            var arrControlsToChangeState = controlsToChangeState.AppendIfNotNull(componentLoading).Concat(controlsToAlsoChangeRenderingState ?? Enumerable.Empty<MyComponentBase>()).ToArray();
            if (!arrControlsToChangeState.Any())
                return;
            if (componentLoading is not null && !componentLoading.InteractionState.V.IsForced)
                componentLoading.InteractionState.ParameterValue = ComponentState.Loading;

            var notifyParamsChangedTasks = arrControlsToChangeState.SelectMany(c => c.Descendants).Concat(arrControlsToChangeState).Distinct().ToDictionary(c => c, c => new Func<Task<MyComponentBase>>(async () => await c.NotifyParametersChangedAsync()));
            var changeStateTasks = new Dictionary<MyComponentBase, Func<Task<MyComponentBase>>>();
            foreach (var control in arrControlsToChangeState)
            {
                var c = control;
                if (c.InteractionState is not null && !c.InteractionState.V.IsForced && c != componentLoading)
                    c.InteractionState.ParameterValue = state;
                if (!changeRenderingState.In(ChangeRenderingStateMode.AllSpecified, ChangeRenderingStateMode.AllSpecifiedThenCurrent))
                    continue;

                changeStateTasks[c] = async () => await c.StateHasChangedAsync(true);
            }

            if (changeRenderingState.In(ChangeRenderingStateMode.Current, ChangeRenderingStateMode.AllSpecifiedThenCurrent))
                changeStateTasks[this] = async () => await StateHasChangedAsync(true);

            ClearControlsRerenderingStatus(changeStateTasks.Keys);
            //Logger.For(GetType()).Info($"{GetType().Name}: SetControlStatesAsync({state}) - notifying {notifyParamsChangedTasks.Keys.Count} controls parameters changed ({notifyParamsChangedTasks.Keys.Select(c => c.GetType().Name).JoinAsString(", ")})");
            await Task.WhenAll(notifyParamsChangedTasks.Values.Select(t => t.Invoke()));
            await Task.WhenAll(changeStateTasks.Values.Select(t => t.Invoke()));
            await WaitForControlsToRerenderAsync(changeStateTasks.Keys);
        }

        public Task SetControlStateAsync(ComponentState state, MyComponentBase controlToChangeState, MyButtonBase btnLoading = null, ChangeRenderingStateMode changeRenderingState = ChangeRenderingStateMode.AllSpecified, IEnumerable<MyComponentBase> controlsToAlsoChangeRenderingState = null) => SetControlStatesAsync(state, controlToChangeState.ToArrayOfOne(), btnLoading, changeRenderingState, controlsToAlsoChangeRenderingState);

        public async Task SetControlStatesAsync(ComponentState state, IEnumerable<IComponent> controlsToChangeState, MyComponentBase componentLoading = null, ChangeRenderingStateMode changeRenderingState = ChangeRenderingStateMode.AllSpecified, IEnumerable<MyComponentBase> controlsToAlsoChangeRenderingState = null)
        {
            var arrControlsToChangeState = controlsToChangeState.ToArray();
            var nativeControls = arrControlsToChangeState.OfType<MyComponentBase>().ToArray();
            var nonNativeControls = arrControlsToChangeState.Except(nativeControls); //.Where(c => c.GetType().BaseType?.GetGenericTypeDefinition() == typeof(TelerikInputBase<>)).ToArray();
            var nncChangeStates = new List<Func<Task>>();
            foreach (var c in nonNativeControls)
            {
                c.SetPropertyValue("Enabled", state.IsEnabledOrForceEnabled);
                nncChangeStates.Add(async () => await c.StateHasChangedAsync());
            }

            await Task.WhenAll(nncChangeStates.Select(t => t.Invoke()));

            await SetControlStatesAsync(state, nativeControls, componentLoading, changeRenderingState, controlsToAlsoChangeRenderingState);
        }

        public Task SetControlStateAsync(ComponentState state, IComponent controlToChangeState, MyButtonBase btnLoading = null, ChangeRenderingStateMode changeRenderingState = ChangeRenderingStateMode.AllSpecified, IEnumerable<MyComponentBase> controlsToAlsoChangeRenderingState = null) => SetControlStatesAsync(state, controlToChangeState.ToArrayOfOne(), btnLoading, changeRenderingState, controlsToAlsoChangeRenderingState);
        
        protected MyComponentBase[] GetInputControls()
        {
            var inputControls = Descendants.Where(c => c is MyInputGroup or MyTextInput or MyPasswordInput or MyDropDownBase or MyButton or MyNavLink or MyCheckBox or MyRadioButtonBase or MyProgressBar or MyFileUpload).ToArray();
            var inputControlsDescendants = inputControls.SelectMany(cc => cc.Descendants).Distinct().ToArray();
            var topMostInputControls = inputControls.Where(c => !c.In(inputControlsDescendants)).ToArray();
            return topMostInputControls;
        }

        protected async Task CatchAllExceptionsAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                await PromptMessageAsync(NotificationType.Error, ex.Message);
            }
        }

        public bool HasClass(string cls) => Classes.Contains(cls);

        public async Task<MyComponentBase> NavigateAndUpdateActiveNavLinksAsync(string url)
        {
            NavigationManager.NavigateTo(url);
            await (await NavBar.ModuleAsync).InvokeVoidAndCatchCancellationAsync("blazor_NavBar_SetNavLinksActiveClasses", url);
            return this;
        }

        [JSInvokable]
        public bool IsDisabled() => InteractionState.V.IsDisabledOrForceDisabled;

        [JSInvokable]
        public bool IsDisabledByGuid(Guid guid) => Layout.Components.Values.OfType<MyButtonBase>().Single(c => c._guid == guid).InteractionState.V.IsDisabledOrForceDisabled;

        public async Task FixNonNativeComponentSyncPaddingGroupAsync(Guid guid) => await (await InputModuleAsync).InvokeVoidAndCatchCancellationAsync("blazor_NonNativeInput_FixInputSyncPaddingGroup", guid);

        protected virtual async Task DisposeAsync(bool disposing)
        {
            if (IsDisposed)
                return;

            if (disposing)
            {
                IsDisposed = true;
                IsCached = false;
                if (LayoutParameter.HasValue())
                {
                    Layout.Components.TryRemove(_guid, out _);
                    Layout.LayoutSessionIdSet -= Layout_SessionIdSet;
                }

                //if (_isCommonLayout && SessionId != Guid.Empty)
                //{
                //    SessionCache[SessionId].CurrentLayout = null;
                //    if (_syncComponentsCache.CurrentCount == 0)
                //        await _syncComponentsCache.ReleaseAsync();
                //    _syncComponentsCache.Dispose();
                //}

                _syncRender?.Dispose();
                _syncAfterSessionIdSet?.Dispose();
                _syncClasses?.Dispose();
                _syncStyles?.Dispose();
                _syncAttributes?.Dispose();
                HttpClient?.Dispose();
            }

            await Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(true);
            GC.SuppressFinalize(this);
        }

        ~MyComponentBase() => _ = DisposeAsync(false);

        public override string ToString() => $"{GetType().Name} [{_guid.ToString().Take(4)}...{_guid.ToString().TakeLast(4)}] {(_renderClasses?.Any() == true ? _renderClasses : "< no classes >")}";

        public bool Equals(MyComponentBase other)
        {
            if (other is null) return false;
            return ReferenceEquals(this, other) || _guid.Equals(other._guid);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((MyComponentBase)obj);
        }

        public override int GetHashCode() => _guid.GetHashCode();
        public static bool operator ==(MyComponentBase left, MyComponentBase right) => Equals(left, right);
        public static bool operator !=(MyComponentBase left, MyComponentBase right) => !Equals(left, right);

        protected virtual Task OnAfterRenderFinishedAsync(bool isFirstRender) => Task.CompletedTask;
        public event MyAsyncEventHandler<MyComponentBase, AfterRenderFinishedEventArgs> AfterRenderFinished;
        private async Task OnAfterRenderFinishingAsync(AfterRenderFinishedEventArgs e)
        {
            await AfterRenderFinished.InvokeAsync(this, e);
            await OnAfterRenderFinishedAsync(e.IsFirstRender);
        }
        private async Task OnAfterRenderFinishingAsync(bool isFirstRender) => await OnAfterRenderFinishingAsync(new AfterRenderFinishedEventArgs(isFirstRender));
        public class AfterRenderFinishedEventArgs : EventArgs
        {
            public bool IsFirstRender { get; }

            public AfterRenderFinishedEventArgs(bool isFirstRender)
            {
                IsFirstRender = isFirstRender;
            }
        }

        protected class ComponentAuthenticationStatus
        {
            public bool AuthenticationPerformed { get; set; }
            public bool AuthenticationChanged { get; set; }
            public bool AuthenticationSuccessful { get; set; }
            public bool AuthenticationNotPerformed => !AuthenticationPerformed;
            public bool AuthenticationNotChanged => !AuthenticationChanged;
            public bool AuthenticationFailed => !AuthenticationSuccessful;

            public string ResponseMessage { get; internal set; }

            public ComponentAuthenticationStatus(bool authenticationPerformed, bool authenticationChanged, bool authenticationSuccessful)
            {
                AuthenticationPerformed = authenticationPerformed;
                AuthenticationChanged = authenticationChanged;
                AuthenticationSuccessful = authenticationSuccessful;
            }

            public ComponentAuthenticationStatus() { }
        }
    }

    public enum ChangeRenderingStateMode
    {
        None,
        AllSpecified,
        Current,
        AllSpecifiedThenCurrent
    }
}
