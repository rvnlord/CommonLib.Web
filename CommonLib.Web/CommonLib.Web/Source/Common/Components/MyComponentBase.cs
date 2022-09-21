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
using CommonLib.Web.Source.Common.Components.MyEditContextComponent;
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
using NBitcoin;

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
        private bool _firstParamSetup;
        private bool _isInitialized;
        [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Disposed asynchronously")]
        private readonly SemaphoreSlim _syncClasses = new(1, 1);
        [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Disposed asynchronously")]
        private readonly SemaphoreSlim _syncStyles = new(1, 1);
        [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Disposed asynchronously")]
        private readonly SemaphoreSlim _syncAttributes = new(1, 1);
        [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Disposed asynchronously")]
        private readonly SemaphoreSlim _syncStateChanged = new(1, 1);
        private readonly OrderedSemaphore _syncRender = new(1, 1);
        private static readonly SemaphoreSlim _syncSessionId = new(1, 1);
        private readonly OrderedSemaphore _syncAfterSessionIdSet = new(1, 1);        
        private readonly OrderedSemaphore _syncComponentCached = new(1, 1);
        private readonly OrderedSemaphore _syncAllComponentsCached = new(1, 1);
        private static readonly OrderedSemaphore _syncComponentsCache = new(1, 1);
        private DateTime _creationTime;
        private MyPromptBase _prompt;
        private Guid _sessionId;
        private bool _sessionIdAlreadySet;

        protected Guid _guid { get; set; }
        protected string _id { get; set; }
        protected string _renderClasses { get; set; } // these properties prevents async component rendering from throwing if clicking sth fast would change the collection before it is iterated properly within the razor file
        protected string _renderStyle { get; set; }
        protected Dictionary<string, object> _renderAttributes { get; } = new();
        protected List<string> _classes { get; } = new();
        protected OrderedDictionary<string, string> _style { get; } = new();
        protected OrderedDictionary<string, string> _attributes { get; } = new();
        protected BlazorParameter<MyComponentBase> _bpParentToCascade { get; set; }

        protected bool _isCommonLayout
        {
            get
            {
                var type = GetType();
                return type.IsSubclassOf(typeof(MyLayoutComponentBase)) && type == typeof(MyLayoutComponent_Layout);
            }
        }

        protected bool _isLayout
        {
            get
            {
                var type = GetType();
                return type.IsSubclassOf(typeof(MyLayoutComponentBase)) && type.Name.In("_Layout", "MainLayout");
            }
        }
        
        public Task<IJSObjectReference> ComponentBaseModuleAsync => _componentBaseModuleAsync ??= MyJsRuntime.ImportComponentOrPageModuleAsync(nameof(MyComponentBase).BeforeLast("Base"), NavigationManager, HttpClient);
        public Task<IJSObjectReference> ModuleAsync => _moduleAsync ??= MyJsRuntime.ImportComponentOrPageModuleAsync(GetType().BaseType?.Name.BeforeLast("Base"), NavigationManager, HttpClient);
        public Task<IJSObjectReference> PromptModuleAsync
        {
            get
            {
                if (_isCommonLayout)
                    return _promptModuleAsync ??= MyJsRuntime.ImportComponentOrPageModuleAsync(nameof(MyPromptBase).BeforeLast("Base"), NavigationManager, HttpClient);
                return LayoutParameter?.ParameterValue.PromptModuleAsync;
            }
        }

        public bool IsRendered => !_firstRenderAfterInit;
        public bool IsDisposed { get; set; }
        public bool FirstParamSetup => _firstParamSetup;

        public Guid SessionId
        {
            get
            {
                if (_sessionId == Guid.Empty && LayoutParameter.ParameterValue is not null && LayoutParameter.ParameterValue.SessionId != Guid.Empty)
                    _sessionId = LayoutParameter.ParameterValue.SessionId;
                return _sessionId;
            }
            set =>  _sessionId = value;
        }

        public bool IsCached { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter(CaptureUnmatchedValues = true)]
        public Dictionary<string, object> AdditionalAttributes { get; set; } = new();

        [CascadingParameter]
        public BlazorParameter<MyEditContext> CascadedEditContext { get; set; }

        [CascadingParameter(Name = "LayoutParameter")]
        public BlazorParameter<MyLayoutComponentBase> LayoutParameter { get;  set; }

        [CascadingParameter(Name = "ParentParameter")]
        public BlazorParameter<MyComponentBase> ParentParameter { get; set; }
        
        public MyLayoutComponentBase Layout => LayoutParameter?.ParameterValue;
        public MyComponentBase Parent => ParentParameter?.ParameterValue;
        public List<MyComponentBase> Children => Layout.Components.Values.Where(c => c.Parent == this).ToList();
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

        public AuthenticateUserVM AuthenticatedUser { get; set; }

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
        public IBackendInfoClient BackendInfoClient { get; set; }

        [Inject]
        public AuthenticationStateProvider AuthStateProvider { get; set; }
        public UserAuthenticationStateProvider UserAuthStateProvider => (UserAuthenticationStateProvider)AuthStateProvider;

        [Inject]
        public IParametersCacheService ParametersCache { get; set; }
        
        [Inject]
        public ISessionStorageService SessionStorage { get; set; }

        [Inject]
        public ISessionCacheService SessionCache { get; set; }

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
                }

                await OnFirstParametersSetAsync();
            }
                
            OnParametersSet();
            await OnParametersSetAsync();
            _firstParamSetup = false;
            SetAllBlazorParametersAsUnchanged();
        }
        
        protected virtual async Task OnFirstParametersSetAsync() => await Task.CompletedTask;
        protected virtual void OnParametersSet() { }
        protected virtual async Task OnParametersSetAsync() => await Task.CompletedTask;

        async Task IHandleAfterRender.OnAfterRenderAsync()
        {
            if (IsDisposed || JsRuntime == null)
                return;

            try
            {
                await _syncRender.WaitAsync(); // if `State` is being changed manually by calling `StateHasChangedAsync` also block render | For instance first render may enter this method and subsequent render can enter as well before the first render finished thus leaving some parts like session not initialized properly

                var isFirstRenderAfterInit = _firstRenderAfterInit;
                if (isFirstRenderAfterInit)
                {
                    _firstRenderAfterInit = false;

                    if (_isCommonLayout)
                    {
                        await SetSessionIdAsync();
                        SessionCache.AddIfNotExistAndGet(SessionId, new SessionCacheData()).CurrentLayout = (MyLayoutComponentBase)this;
                        await PromptModuleAsync; // this makes prompt js available within any component
                        var prompts = await ComponentsByTypeAsync<MyPromptBase>();
                        foreach (var prompt in prompts)
                            await prompt.StateHasChangedAsync();
                        await ((MyLayoutComponentBase)this).OnLayoutSessionIdSettingAsync(SessionId);
                    }

                    await OnAfterFirstRenderAsync();
                }

                OnAfterRender(isFirstRenderAfterInit);
                await OnAfterRenderAsync(isFirstRenderAfterInit);
                await OnAfterRenderFinishingAsync(isFirstRenderAfterInit);

                if (LayoutParameter.HasValue() && !_isCommonLayout && SessionId != Guid.Empty && isFirstRenderAfterInit)
                    await Layout_SessionIdSet(null, new MyLayoutComponentBase.LayoutSessionIdSetEventArgs(SessionId), CancellationToken.None);
            }
            catch (Exception ex) when (ex is TaskCanceledException or ObjectDisposedException)
            {
                //Logger.For<MyComponentBase>().Warn("'OnAfterRenderAsync' was canceled, disposed component?");
            }
            catch (Exception)
            {
                if (!IsDisposed)
                    throw;
            }
            finally
            {
                if (_syncRender.CurrentCount == 0) // Release render if we are changing `State` manually so `StateHasChanged` knows about it
                    await _syncRender.ReleaseAsync();
            }
        }

        private async Task Layout_SessionIdSet(MyComponentBase sender, MyLayoutComponentBase.LayoutSessionIdSetEventArgs e, CancellationToken token)
        {
            await _syncAfterSessionIdSet.WaitAsync();

            if (IsDisposed || _isCommonLayout || Layout is null || _sessionIdAlreadySet)
            {
                await _syncAfterSessionIdSet.ReleaseAsync();
                return;
            }

            _sessionIdAlreadySet = true;
            await OnLayoutSessionIdSetAsync(e.Sessionid);
            
            await _syncAfterSessionIdSet.ReleaseAsync();
        }
        
        protected virtual async Task OnAfterFirstRenderAsync() => await Task.CompletedTask;
        protected virtual void OnAfterRender(bool firstRender) { }
        protected virtual async Task OnAfterRenderAsync(bool firstRender) => await Task.CompletedTask;

        protected virtual async Task OnLayoutSessionIdSetAsync(Guid sessionId) => await Task.CompletedTask;

        protected virtual bool ShouldRender() => true;

        protected Task InvokeAsync(Action workItem) => _renderHandle.Dispatcher.InvokeAsync(workItem);

        protected Task InvokeAsync(Func<Task> workItem) => _renderHandle.Dispatcher.InvokeAsync(workItem);

        protected bool IsFirstParamSetup() => _firstParamSetup;

        protected bool HasAuthenticationStatus(AuthStatus authStatus) => AuthenticatedUser == null && authStatus == AuthStatus.NotChecked || AuthenticatedUser != null && AuthenticatedUser.HasAuthenticationStatus(authStatus);
        
        protected bool HasAnyAuthenticationStatus(params AuthStatus[] authStatuses) => AuthenticatedUser == null && AuthStatus.NotChecked.In(authStatuses) || AuthenticatedUser != null && AuthenticatedUser.HasAnyAuthenticationStatus(authStatuses);

        protected async Task<bool> EnsureAuthenticatedAsync()
        {
            var navBar = await ComponentByTypeAsync<MyNavBarBase>();
            var authResponse = await AccountClient.GetAuthenticatedUserAsync();
            var prevAuthUser = Mapper.Map(AuthenticatedUser, new AuthenticateUserVM());
            AuthenticatedUser = authResponse.Result;
            if (!authResponse.IsError && authResponse.Result.HasAuthenticationStatus(AuthStatus.Authenticated))
            {
                if (!AuthenticatedUser.Equals(prevAuthUser)) // to rpeveeent infinite AftereRender loop
                {
                    await StateHasChangedAsync(true);
                    await navBar.StateHasChangedAsync(true);
                }
                return true;
            }

            await PromptMessageAsync(NotificationType.Error, authResponse.Message);
            if (!AuthenticatedUser.Equals(prevAuthUser))
            {
                await StateHasChangedAsync(true);
                await navBar.StateHasChangedAsync(true);
            }
            return false;
        }

        protected async Task<bool> EnsureAuthenticationPerformedAsync()
        {
            var navBar = await ComponentByTypeAsync<MyNavBarBase>();
            var authResponse = await AccountClient.GetAuthenticatedUserAsync();
            var prevAuthUser = Mapper.Map(AuthenticatedUser, new AuthenticateUserVM());
            AuthenticatedUser = authResponse.Result;
            if (!authResponse.IsError) 
            {
                if (!AuthenticatedUser.Equals(prevAuthUser))
                {
                    await StateHasChangedAsync(true);
                    await navBar.StateHasChangedAsync(true);
                }

                return true;
            }

            await PromptMessageAsync(NotificationType.Error, authResponse.Message);
            if (!AuthenticatedUser.Equals(prevAuthUser))
            {
                await StateHasChangedAsync(true);
                await navBar.StateHasChangedAsync(true);
            }
            return false;
        }

        protected void SetMainCustomAndUserDefinedClasses(string mainClass, IEnumerable<string> customClasses, bool preserveExistingClasses = false)
        {
            if (IsDisposed)
                return;

            _syncClasses.Wait();

            if (!preserveExistingClasses)
                _classes.Clear();
            if (!mainClass.IsNullOrWhiteSpace())
                _classes.Add(mainClass);
            if (customClasses != null)
                _classes.AddRange(customClasses.Where(c => !c.IsNullOrWhiteSpace()));
            var additionalClasses = AdditionalAttributes.VorN("class")?.ToString().NullifyIf(s => s.IsNullOrWhiteSpace())?.Split(" ");
            if (additionalClasses != null)
                _classes.AddRange(additionalClasses.Where(c => !c.IsNullOrWhiteSpace()));
            _renderClasses = _classes.Distinct().JoinAsString(" ");

            _syncClasses.Release();
        }

        protected void SetMainAndUserDefinedClasses(string mainClass, bool preserveExistingClasses = false) => SetMainCustomAndUserDefinedClasses(mainClass, null, preserveExistingClasses);
        protected void SetUserDefinedClasses(bool preserveExistingClasses = false) => SetMainCustomAndUserDefinedClasses(null, null, preserveExistingClasses);

        protected void AddClasses(string cls, params string[] classes)
        {
            if (IsDisposed)
                return;

            _syncClasses.Wait();

            if (classes != null)
                _classes.AddRange(classes.Prepend(cls).Where(c => !c.IsNullOrWhiteSpace()));
            _renderClasses = _classes.Distinct().JoinAsString(" ");

            _syncClasses.Release();
        }

        protected void AddClasses(IEnumerable<string> classes)
        {
            if (IsDisposed)
                return;

            _syncClasses.Wait();

            if (classes != null)
                _classes.AddRange(classes.Where(c => !c.IsNullOrWhiteSpace()));
            _renderClasses = _classes.Distinct().JoinAsString(" ");

            _syncClasses.Release();
        }

        protected void AddClass(string cls) => AddClasses(cls);

        protected void RemoveClasses(string cls, params string[] classes)
        {
            RemoveClasses(classes.Prepend(cls).ToArray());
        }

        protected void RemoveClasses(string[] classes)
        {
            if (IsDisposed)
                return;

            _syncClasses.Wait();

            _classes.RemoveAll(s => s.In(classes));
            _renderClasses = _classes.JoinAsString(" ");

            _syncClasses.Release();
        }

        protected void RemoveClass(string cls) => RemoveClasses(cls);

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

        protected void StateHasChanged()
        {
            if (_hasPendingQueuedRender)
                return;

            if (!IsRendered || ShouldRender() || _renderHandle.IsRenderingOnMetadataUpdate)
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
        }

        public async Task<MyComponentBase> StateHasChangedAsync(bool force = false)
        {
            if (force)
                ShouldRender();
            
            await InvokeAsync(StateHasChanged);
            
            return this;
        }

        public async Task<MyComponentBase> WaitForRenderAsync()
        {
            await _syncStateChanged.WaitAsync();
            //await TaskUtils.WaitUntil(() => _syncRender.CurrentCount == 0); // wait for aquiring lock in `AfterRender`
            await TaskUtils.WaitUntil(() => _syncStateChanged.CurrentCount == 1); // wait for releasing `StateChanged` lock which merging `StateChanged` with `AfterRender`
            await TaskUtils.WaitUntil(() => _syncRender.CurrentCount == 1); // wait for entire `AfterRender` to complete

            return this;
        }

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
            await _syncSessionId.WaitAsync();
            
            if (SessionId == Guid.Empty)
                SessionId = await SessionStorage.GetOrCreateSessionIdAsync();

            _syncSessionId.Release();
        }

        public async Task<Guid> GetSessionIdAsync()
        {
            if (SessionId == Guid.Empty) 
                SessionId = await SessionStorage.GetSessionIdAsync();
            
            return SessionId;
        }

        public async Task<Guid> GetSessionIdOrEmptyAsync()
        {
            if (SessionId == Guid.Empty)
            {
                var sessionid = await SessionStorage.GetSessionIdOrEmptyAsync();
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
            await _syncComponentsCache.WaitAsync();
            var componentsByClass = (Layout ?? (MyLayoutComponentBase)this).Components.Values.OfType<TComponent>().Where(c => cssClass.In(c._classes)).ToList();
            await _syncComponentsCache.ReleaseAsync();
            return componentsByClass;
        }

        public async Task<TComponent> ComponentByClassAsync<TComponent>(string cssClass) where TComponent : MyComponentBase
        {
            return (await ComponentsByClassAsync<TComponent>(cssClass)).Single();
        }

        public async Task<TComponent> ComponentByIdAsync<TComponent>(string id) where TComponent : MyComponentBase
        {
            await _syncComponentsCache.WaitAsync();
            var componentById = (Layout ?? (MyLayoutComponentBase)this).Components.Values.ToArray().OfType<TComponent>().Single(c => id.EqualsInvariant(c._id));
            await _syncComponentsCache.ReleaseAsync();
            return componentById;
        }

        public async Task<TComponent> ComponentByGuidAsync<TComponent>(Guid guid) where TComponent : MyComponentBase
        {
            await _syncComponentsCache.WaitAsync();
            var componentByGuid = (Layout ?? (MyLayoutComponentBase)this).Components.Values.ToArray().OfType<TComponent>().Single(c => guid.Equals(c._guid));
            await _syncComponentsCache.ReleaseAsync();
            return componentByGuid;
        }

        public async Task<List<TComponent>> ComponentsByTypeAsync<TComponent>() where TComponent : MyComponentBase
        {
            await _syncComponentsCache.WaitAsync();
            var componentsByType = (Layout ?? (MyLayoutComponentBase)this).Components.Values.ToArray().OfType<TComponent>().ToList();
            await _syncComponentsCache.ReleaseAsync();
            return componentsByType;
        }

        public async Task<TComponent> ComponentByTypeAsync<TComponent>() where TComponent : MyComponentBase
        {
            return (await ComponentsByTypeAsync<TComponent>()).Single();
        }
        
        public async Task ShowLoginModalAsync() => await ComponentByClassAsync<MyModalBase>("my-login-modal").ShowModalAsync();

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
                    LayoutParameter.ParameterValue.Components.Remove(_guid);
                    LayoutParameter.ParameterValue.LayoutSessionIdSet -= Layout_SessionIdSet;
                }

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
        
        public override string ToString() => $"[{_guid}] {_renderClasses}";

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

    }
}
