using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazored.SessionStorage;
using CommonLib.Web.Source.Common.Components.MyNavBarComponent;
using CommonLib.Web.Source.Common.Components.MyNavItemComponent;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Services.Interfaces;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils;
using CommonLib.Web.Source.Common.Components.MyEditContextComponent;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils;
using CommonLib.Web.Source.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace CommonLib.Web.Source.Common.Components.MyNavLinkComponent
{
    public class MyNavLinkBase : MyComponentBase
    {
        protected BlazorParameter<NavLinkState?> _bpState;
        protected NavLinkState? _state;

        protected ElementReference _jsNavLink { get; set; }
        protected IconType _openIcon { get; set; }
        protected IconType _closeIcon { get; set; }
        protected IconType _closeIconXs { get; set; }
        protected IconType _openIconXs { get; set; }
        protected IconType _icon { get; set; }

        protected string _absoluteVirtualLink { get; set; }
        //protected DotNetObjectReference<MyNavLinkBase> _navLinkDotNetRef { get; set; }

        [Inject]
        public IJQueryService IjQueryServiceService { get; set; }

        [CascadingParameter]
        public IconType CascadedIcon { get; set; }

        [Parameter]
        public IconType Icon { get; set; }

        [Parameter]
        public string To { get; set; }

        [Parameter]
        public BlazorParameter<NavLinkState?> State { get; set; }

        [Parameter]
        public NavLinkIconPlacement IconPlacement { get; set; } = NavLinkIconPlacement.Left;

        [Parameter]
        public bool MatchEmptyRoute { get; set; }

        [CascadingParameter]
        public MyNavBar NavBar { get; set; }

        [CascadingParameter]
        public NavItemType NavItemType { get; set; }
        
        protected override async Task OnInitializedAsync()
        {
            _bpState ??= new BlazorParameter<NavLinkState?>(_state);

            await Task.CompletedTask;
        }

        protected override async Task OnParametersSetAsync()
        {
            if (IsFirstParamSetup())
            {
                if (NavItemType.In(NavItemType.Link, NavItemType.Home))
                    _absoluteVirtualLink = To == null ? null : PathUtils.Combine(PathSeparator.FSlash, NavigationManager.BaseUri, To);
                
                SetMainAndUserDefinedClasses("my-nav-link");
                SetUserDefinedStyles();
                SetUserDefinedAttributes();

                _icon = Icon ?? CascadedIcon;
                _openIcon = NavItemType switch
                {
                    NavItemType.Link => null,
                    NavItemType.DropDown => IconType.From(LightIconType.ChevronCircleDown),
                    NavItemType.DropUp => IconType.From(LightIconType.ChevronCircleUp),
                    NavItemType.DropLeft => IconType.From(LightIconType.ChevronCircleLeft),
                    NavItemType.DropRight => IconType.From(LightIconType.ChevronCircleRight),
                    _ => null
                };
                _closeIcon = NavItemType switch
                {
                    NavItemType.Link => null,
                    NavItemType.DropDown => IconType.From(LightIconType.ChevronCircleUp),
                    NavItemType.DropUp => IconType.From(LightIconType.ChevronCircleDown),
                    NavItemType.DropLeft => IconType.From(LightIconType.ChevronCircleRight),
                    NavItemType.DropRight => IconType.From(LightIconType.ChevronCircleLeft),
                    _ => null
                };
                _openIconXs = NavItemType == NavItemType.Link ? null : IconType.From(LightIconType.ChevronCircleDown);
                _closeIconXs = NavItemType == NavItemType.Link ? null : IconType.From(LightIconType.ChevronCircleUp);
            }

            CascadedEditContext.BindValidationStateChanged(CurrentEditContext_ValidationStateChangedAsync);

            if (State.HasChanged())
            {
                _state = State.ParameterValue ?? NavLinkState.Enabled;

                if (_state == NavLinkState.Disabled)
                    AddClass("disabled");
                else
                    RemoveClasses("disabled");
            }
            
            await Task.CompletedTask;
        }
        
        protected override async Task OnAfterFirstRenderAsync() // this is executed before oute component after render but the outer component won't wait until this is finished unless forced
        {
            //Logger.For<MyNavLinkBase>().Info($"'OnAfterFirstRenderAfterInitAsync()' started");
            //_navLinkDotNetRef = DotNetObjectReference.Create(this);
            //Logger.For<MyNavLinkBase>().Info($"Adding .NET Ref for '{_guid}'");
            //try
            //{
            //    await (await ModuleAsync).InvokeVoidAsync("blazor_NavLink_AfterRender", _navLinkDotNetRef, _guid);
            //}
            //catch (ObjectDisposedException)
            //{
            //    Logger.For<MyNavLinkBase>().Info("NavLink JS Module has already been disposed (did you change the page before the previous one was fully loaded?)");
            //}
            //Logger.For<MyNavLinkBase>().Info($"'OnAfterFirstRenderAfterInitAsync()' finished");
            await Task.CompletedTask;
        }

        [JSInvokable] // to fix no navigation on clicking some `a` tags while using `@onclick="NavLink_ClickAsync"`
        public static async Task NavLink_ClickAsync(Guid guid, Guid sessionId) // MouseEventArgs e
        {
            //var (cache, cacheScope) = WebUtils.GetScopedService<IComponentsCacheService>();
            var cache = WebUtils.GetService<IComponentsCacheService>();

            //var t1 = cache.SessionCache.ElementAtOrDefault(0).Value?.Components.Values.OfType<MyNavLinkBase>().FirstOrDefault(nl => nl._guid == guid);
            //var t2 = cache.SessionCache.ElementAtOrDefault(1).Value?.Components.Values.OfType<MyNavLinkBase>().FirstOrDefault(nl => nl._guid == guid);
            //var t3 = cache.SessionCache.ElementAtOrDefault(2).Value?.Components.Values.OfType<MyNavLinkBase>().FirstOrDefault(nl => nl._guid == guid);

            var navLink = cache.SessionCache[sessionId].Components.Values.OfType<MyNavLinkBase>().Single(nl => nl._guid == guid);
            navLink.NavigationManager.NavigateTo(navLink._absoluteVirtualLink);

            //await cacheScope.DisposeScopeAsync();

            await Task.CompletedTask;
        }

        private async Task CurrentEditContext_ValidationStateChangedAsync(object sender, MyValidationStateChangedEventArgs e)
        {
            if (e == null)
                throw new NullReferenceException(nameof(e));
            if (e.ValidationMode == ValidationMode.Property && e.ValidatedFields == null)
                throw new NullReferenceException(nameof(e.ValidatedFields));
            
            if (e.ValidationMode == ValidationMode.Model)
            {
                State = e.ValidationStatus.In(ValidationStatus.Pending, ValidationStatus.Success) 
                    ? NavLinkState.Disabled 
                    : NavLinkState.Enabled;
                await NotifyParametersChangedAsync();
                await StateHasChangedAsync(true);
            }
        }

        //[JSInvokable] // to fix no navigation on clicking some `a` tags while using `@onclick="NavLink_ClickAsync"`
        //public async Task NavLink_ClickAsync() // MouseEventArgs e
        //{
        //    //if (e == null)
        //    //    throw new NullReferenceException(nameof(e));
        //    //if (e.Button != 0)
        //    //    return;

        //    NavigationManager.NavigateTo(_absoluteVirtualLink);

        //    if (NavBar == null || NavBar.RunPureJavascriptVersion)
        //        return; // handled with jQuery event

        //    var navLink = await JQueryService.QueryOneAsync(_jsNavLink).ConfigureAwait(false);
        //    var navItem = await navLink.ClosestAsync(".my-nav-item").ConfigureAwait(false);
        //    if (!(await navItem.ClassesAsync().ConfigureAwait(false)).Any(c => c.StartsWithInvariant("my-drop")))
        //        return;

        //    await NavBar.SyncNavBarAnimsCreation.WaitAsync().ConfigureAwait(false);

        //    var navMenu = await navItem.ChildrenAsync(".my-nav-menu").FirstAsync().ConfigureAwait(false);
        //    var navMenuAncestorsIfAny = await navItem.ParentsUntilAsync(".my-navbar").FilterAsync(".my-nav-menu").ConfigureAwait(false);
        //    var otherNavMenus = await navLink.ClosestAsync(".my-navbar").FindAsync(".my-nav-menu").NotAsync(navMenu).NotAsync(navMenuAncestorsIfAny).ConfigureAwait(false);
        //    var arrOtherNavMenusToHide = await otherNavMenus.FilterAsync(".shown").ConfigureAwait(false);
        //    var dropClass = (await navItem.ClassesAsync().ConfigureAwait(false)).Single(c => c.StartsWithInvariant("my-drop"));
        //    var show = !(await navMenu.IsAsync(".shown").ConfigureAwait(false));

        //    await navMenu.ToggleClassAsync("shown").ConfigureAwait(false);
        //    await otherNavMenus.RemoveClassAsync("shown").ConfigureAwait(false);

        //    await NavBar.FinishAndRemoveRunningAnimsAsync().ConfigureAwait(false);
        //    await NavBar.PrepareNavMenuAsync(navLink, dropClass).ConfigureAwait(false);
        //    await NavBar.CreateToggleNmAnimAsync(navMenu, show, dropClass).ConfigureAwait(false);
        //    await NavBar.CreateHideOnmAnimAsync(arrOtherNavMenusToHide).ConfigureAwait(false);
        //    await NavBar.CreateToggleNmOcIconAnimAsync(navLink, show).ConfigureAwait(false);
        //    await NavBar.CreateHideOnmOcIconAnimAsync(arrOtherNavMenusToHide).ConfigureAwait(false);
        //    await NavBar.RunAnimsAsync().ConfigureAwait(false);

        //    NavBar.SyncNavBarAnimsCreation.Release();
        //}
    }

    public enum NavLinkState
    {
        Enabled,
        Disabled
    }

    public enum NavLinkIconPlacement
    {
        Left,
        Right
    }
}
