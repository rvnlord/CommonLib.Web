using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using Truncon.Collections;
using Microsoft.AspNetCore.Internal;

namespace CommonLib.Web.Source.Common.Components
{
    public abstract class MyComponentBase : IDisposable, IComponent, IHandleEvent, IHandleAfterRender // LayoutComponentBase
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
        private readonly SemaphoreSlim _syncClasses = new(1, 1);
        private readonly SemaphoreSlim _syncStyles = new(1, 1);
        private readonly SemaphoreSlim _syncAttributes = new(1, 1);
        private readonly SemaphoreSlim _syncStateChanged = new(1, 1);
        private readonly SemaphoreSlim _syncRender = new(1, 1);
        private bool _changingState;
        private MyComponentBase _layout;
        private DateTime _creationTime;
        private MyPromptBase _prompt;

        protected Guid _guid { get; set; }
        protected string _id { get; set; }
        protected string _renderClasses { get; set; } // this properties prevents async component rendering from throwing if clicking sth fast would change the collection before it is iterated properly within the razor file
        protected string _renderStyle { get; set; }
        protected Dictionary<string, object> _renderAttributes { get; } = new();
        protected List<string> _classes { get; } = new();
        protected OrderedDictionary<string, string> _style { get; } = new();
        protected OrderedDictionary<string, string> _attributes { get; } = new();
        protected bool _isLayout { get; set; }

        internal const string BodyPropertyName = nameof(Body);

        public Task<IJSObjectReference> ComponentBaseModuleAsync => _componentBaseModuleAsync ??= MyJsRuntime.ImportComponentOrPageModuleAsync(nameof(MyComponentBase).BeforeLast("Base"), NavigationManager, HttpClient);
        public Task<IJSObjectReference> PromptModuleAsync => _promptModuleAsync ??= MyJsRuntime.ImportComponentOrPageModuleAsync(nameof(MyPromptBase).BeforeLast("Base"), NavigationManager, HttpClient);
        public Task<IJSObjectReference> ModuleAsync => _moduleAsync ??= MyJsRuntime.ImportComponentOrPageModuleAsync(GetType().BaseType?.Name.BeforeLast("Base"), NavigationManager, HttpClient);
        public bool IsRendered => !_firstRenderAfterInit;
        public bool IsDisposed { get; set; }
        public bool FirstParamSetup => _firstParamSetup;
        public MyPromptBase Prompt => _prompt ??= ComponentsCache.Components.Values.OfType<MyPromptBase>().Single();
        
        [Parameter]
        public RenderFragment Body { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter(CaptureUnmatchedValues = true)]
        public Dictionary<string, object> AdditionalAttributes { get; set; } = new();

        [CascadingParameter]
        public BlazorParameter<MyEditContext> CascadedEditContext { get; set; }

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
        public IComponentsCacheService ComponentsCache { get; set; }

        protected MyComponentBase()
        {
            _renderFragment = builder =>
            {
                _hasPendingQueuedRender = false;
                BuildRenderTree(builder);
                //OnStateChangedAsync += MyComponentBase_StateChangedAsync;
            };
        }
        
        protected virtual void BuildRenderTree(RenderTreeBuilder builder) { } // code within this class should *not* invoke BuildRenderTree directly, use `_renderFragment` instead

        public Task HandleEventAsync(EventCallbackWorkItem callback, object arg) => callback.InvokeAsync(arg);

        //[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(LayoutComponentBase))]
        public virtual async Task SetParametersAsync(ParameterView parameters)
        {
            parameters.SetParameterProperties(this);

            if (IsDisposed || JsRuntime == null)
                return;

            try
            {
                await InitializeAsync();
                await SetParametersAsync();
                
                await StateHasChangedAsync();
            }
            catch (Exception ex) when (ex is TaskCanceledException or ObjectDisposedException)
            {
                Logger.For<MyComponentBase>().Warn("'SetParametersAsync' was canceled, disposed component?");
            }
        }
        
        private async Task InitializeAsync()
        {
            if (!_isInitialized)
            {
                if (_guid == Guid.Empty) // OnInitializedAsync runs twice by default, once for pre-render and once for the actual render | fixed by using IComponent interface directly
                {
                    _guid = Guid.NewGuid();
                    ComponentsCache.Components[_guid] = this;
                }

                RebuildComponentsCacheOnCrash();
                
                SetAllParametersToDefaults();
                
                OnInitialized();
                await OnInitializedAsync();

                _isInitialized = true;
                _firstRenderAfterInit = true;
                _firstParamSetup = true;
            }
        }

        protected virtual void OnInitialized() { }
        protected virtual async Task OnInitializedAsync() => await Task.CompletedTask;

        private async Task SetParametersAsync()
        {
            // Set Parameters
            if (_firstParamSetup)
            {
                SetCascadingBlazorParametersAsChanged();
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
                //await _syncRender.WaitAsync(); // if `State` is being changed manually by calling `StateHasChangedAsync` // also block render

                //if (_syncStateChanged.CurrentCount == 0) // if we are waiting for render in `WaitForRenderAsync`
                //    _syncStateChanged.Release();

                if (!_isInitialized)
                {
                    await OnAfterPreRenderAsync();
                    return;
                }

                if (_firstRenderAfterInit)
                {
                    _firstRenderAfterInit = false;
                    await PromptModuleAsync; // this makes prompt js available within any component
                    await OnAfterFirstRenderAsync();
                }
                
                OnAfterRender(_firstRenderAfterInit);
                await OnAfterRenderAsync(_firstRenderAfterInit);
            }
            catch (TaskCanceledException)
            {
                Logger.For<MyComponentBase>().Warn("'OnAfterRenderAsync' was canceled, disposed component?");
            }
            finally
            {
                //if (_syncRender.CurrentCount == 0) // Release render if we are changing `State` manually so `StateHasChanged` knows about it
                //    _syncRender.Release();
            }
        }

        protected virtual async Task OnAfterPreRenderAsync() => await Task.CompletedTask; // TODO: this is never called?
        protected virtual async Task OnAfterFirstRenderAsync() => await Task.CompletedTask;
        protected virtual void OnAfterRender(bool firstRender) { }
        protected virtual async Task OnAfterRenderAsync(bool firstRender) => await Task.CompletedTask;

        protected virtual bool ShouldRender() => true;

        protected Task InvokeAsync(Action workItem) => _renderHandle.Dispatcher.InvokeAsync(workItem);

        protected Task InvokeAsync(Func<Task> workItem) => _renderHandle.Dispatcher.InvokeAsync(workItem);

        protected bool IsFirstParamSetup() => _firstParamSetup;

        protected bool IsAuthenticated() => AuthenticatedUser?.IsAuthenticated == true;

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
            if (IsDisposed)
                return;

            _syncClasses.Wait();

            _classes.RemoveAll(s => s.In(classes.Prepend(cls)));
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

        private void SetAllParametersToDefaults()
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
            //if (!_changingState)
            //{
            //    await _syncStateChanged.WaitAsync();
            //    //_changingState = true;
            //}
            
            //if (_syncStateChanged.CurrentCount == 0)
            //    _syncStateChanged.Release();
            //await _syncStateChanged.WaitAsync();

            if (force)
                ShouldRender();
            
            await InvokeAsync(StateHasChanged);



            //_syncStateChanged.Release();

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

        public async Task<MyComponentBase> NotifyParametersChangedAsync()
        {
            await SetParametersAsync(); // for when params were not really changed but their values were
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
            await Prompt.ShowNotificationAsync(status, message);

            //var prompts = ComponentsCache.Components.Values.OfType<MyPromptBase>().ToList();
            //foreach (var prompt in prompts)
            //{
            //    //prompt.Notifications.ParameterValue.Add(new Notification
            //    //{
            //    //    Type = status,
            //    //    Message = message
            //    //});
            //    //prompt.Notifications.SetAsChanged();
            //    //await prompt.NotifyParametersChangedAsync().StateHasChangedAsync(true);
            //}
        }

        private void RebuildComponentsCacheOnCrash()
        {
            _isLayout = GetType().Name.ContainsInvariant("Layout");
            _creationTime = DateTime.UtcNow;

            if (_isLayout) 
                return;

            _layout = this;
            var layouts = ComponentsCache.Components.Values.Where(c => c._isLayout).ToArray();

            if (layouts.Length <= 1) 
                return;
            
            var correctLayout = layouts.MaxBy(l => l._creationTime);
            var incorrectLayouts = layouts.Except(correctLayout).ToArray();

            foreach (var incorrectLayout in incorrectLayouts)
            {
                incorrectLayout.Dispose();
                ComponentsCache.Components.Remove(incorrectLayout._guid);
            }
                    
            var componentsWithWrongLayout = ComponentsCache.Components.Values.Where(c => !c._isLayout && !c._layout.Equals(correctLayout)).ToArray();
            foreach (var componentWithWrongLayout in componentsWithWrongLayout)
            {
                componentWithWrongLayout.Dispose();
                ComponentsCache.Components.Remove(componentWithWrongLayout._guid);
            }
        }

        public List<TComponent> ComponentsByClass<TComponent>(string cssClass) where TComponent : MyComponentBase
        {
            return ComponentsCache.Components.Values.OfType<TComponent>().Where(c => cssClass.In(c._classes)).ToList();
        }

        public TComponent ComponentByClass<TComponent>(string cssClass) where TComponent : MyComponentBase
        {
            return ComponentsByClass<TComponent>(cssClass).Single();
        }

        public TComponent ComponentById<TComponent>(string id) where TComponent : MyComponentBase
        {
            return ComponentsCache.Components.Values.OfType<TComponent>().Single(c => id.EqualsInvariant(c._id));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (disposing)
            {
                IsDisposed = true;
                ComponentsCache.Components.RemoveAll(c => c.Value.Equals(this));
                //if (_moduleAsync != null)
                //{
                //    var module = await _moduleAsync;
                //    try { await module.DisposeAsync(); } catch (JSDisconnectedException) { } 
                //}
                //_moduleAsync?.Dispose();

                //if (_componentBaseModuleAsync != null)
                //{
                //    var module = await _componentBaseModuleAsync;
                //    try { await module.DisposeAsync(); } catch (JSDisconnectedException) { } 
                //}
                //_componentBaseModuleAsync?.Dispose();

                //if (_promptModuleAsync != null)
                //{
                //    var module = await _promptModuleAsync;
                //    try { await module.DisposeAsync(); } catch (JSDisconnectedException) { } 
                //}
                //_promptModuleAsync?.Dispose();

                _syncClasses?.Dispose();
                _syncStyles?.Dispose();
                _syncAttributes?.Dispose();

                HttpClient?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MyComponentBase() => Dispose(false);

        public override string ToString() => $"[{_guid}] {_renderClasses}";

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            return _guid.Equals(((MyComponentBase)obj)._guid);
        }

        public override int GetHashCode() => _guid.GetHashCode();
    }
}
