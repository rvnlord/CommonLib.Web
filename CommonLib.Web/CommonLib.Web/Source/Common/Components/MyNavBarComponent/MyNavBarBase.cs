using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Components.MyModalComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Extensions.Collections;
using CommonLib.Web.Source.Common.Utils;
using CommonLib.Web.Source.Models;
using CommonLib.Web.Source.Models.Interfaces;
using CommonLib.Web.Source.Services;
using CommonLib.Web.Source.Services.Interfaces;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components.MyNavItemComponent;
using CommonLib.Web.Source.Common.Components.MyNavLinkComponent;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.ViewModels.Account;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CommonLib.Web.Source.Common.Components.MyNavBarComponent
{
    public class MyNavBarBase : MyComponentBase
    {
        public bool IsSetupCompleted { get; set; }

        //protected string _hidden { get; set; }
        protected DotNetObjectReference<MyNavBarBase> _navBarDotNetRef { get; set; }
        //protected Task<IJSObjectReference> _modalModuleAsync;
        
        public ConcurrentBag<IAnimeJs> NavBarAnims { get; } = new ConcurrentBag<Models.Interfaces.IAnimeJs>();
        public SemaphoreSlim SyncNavBarAnimsCreation { get; } = new SemaphoreSlim(1, 1);
        public bool RunPureJavascriptVersion => true;
        //public Task<IJSObjectReference> ModalModuleAsync => _modalModuleAsync ??= MyJsRuntime.ImportComponentOrPageModuleAsync(nameof(MyModal), NavigationManager, HttpClient);

        [Inject]
        public IJQueryService JQuery { get; set; }

        [Inject] 
        public IAnimeJsService AnimeJsService { get; set; }

        [Parameter]
        public string Title { get; set; }

        [Parameter]
        public bool Sticky { get; set; }
        
        public Guid LoginModalGuid { get; set; }

        protected override async Task OnInitializedAsync() // init gets caller twice, once for pre-render and once for the actual render, don't initialize guids here
        {
            //Logger.For<MyNavBarBase>().Info("'OnInitializedAsync()' started");
            if (LoginModalGuid == Guid.Empty)
                LoginModalGuid = Guid.NewGuid();
            //Layout.SetProperty("NavBar", this);
            //Logger.For<MyNavBarBase>().Info("'NavBar' property for Layout set");
            await Task.CompletedTask;
            //Logger.For<MyNavBarBase>().Info("'OnInitializedAsync()' finished");
        }

        protected override async Task OnAfterFirstRenderAsync()
        {
            //if (!firstRender)
            //    return;

            //var css = await JQueryService.QueryAsync(".my-nav-link-content").FirstAsync().CssAsync("color", "red").ConfigureAwait(false);
            
            //Logger.For<MyNavBarBase>().Info("'OnAfterFirstRenderAfterInitAsync()' started");

            if (RunPureJavascriptVersion)
            {
                //await MyJsRuntime.JsVoidFromComponent(nameof(MyNavBar), "blazor_NavBar_AfterRender");
                await (await ModuleAsync).InvokeVoidAsync("blazor_NavBar_AfterFirstRender");
                var prevAuthUser = Mapper.Map(AuthenticatedUser, new AuthenticateUserVM()); // to prevent  
                AuthenticatedUser = (await AccountClient.GetAuthenticatedUserAsync()).Result;
                if (!AuthenticatedUser.Equals(prevAuthUser))
                    await StateHasChangedAsync();
                //Logger.For<MyNavBarBase>().Info("JS 'blazor_NavBar_AfterRender' executed");
            }
            else
            { // don't use the code below, content haven't been modified for NavBar updates
                //var t = await JQueryService.QueryOneAsync(".my-navbar").AttrAsync("class");
                _navBarDotNetRef = DotNetObjectReference.Create(this);
                AnimeJsService.DotNetRef = _navBarDotNetRef;

                await SyncNavBarAnimsCreation.WaitAsync().ConfigureAwait(false);

                await FinishAndRemoveRunningAnimsAsync().ConfigureAwait(false);
                await AdjustNavMenusToDeviceSizeAsync().ConfigureAwait(false);

                IJQueryService.OnWindowResizingAsync += NavBar_WindowResized;

                SyncNavBarAnimsCreation.Release();
            }

            //Logger.For<MyNavBarBase>().Info("'OnAfterFirstRenderAfterInitAsync()' finished");
        }

        protected override async Task OnParametersSetAsync()
        {
            if (IsFirstParamSetup())
            {
                Logger.For<MyNavBarBase>().Info("'OnParametersSetAsync() - First Setup' started");

                var customClasses = new List<string>();
                if (Sticky)
                    customClasses.Add("my-sticky");

                SetMainCustomAndUserDefinedClasses("my-navbar", customClasses);
                SetUserDefinedStyles();
                SetUserDefinedAttributes();
                
                Logger.For<MyNavBarBase>().Info("'NavLinks Dictionary' initialized");

                Logger.For<MyNavBarBase>().Info("'OnParametersSetAsync() - First Setup' finished");
            }
            
            await Task.CompletedTask;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender || IsDisposed)
                return;
            
            //var navitems = Descendants.OfType<MyNavItemBase>().Where(c => c.InteractionState.V.State != ComponentStateKind.Enabled).ToArray();
            //await SetControlStatesAsync(ComponentState.Enabled, navitems);

            await (await ModuleAsync).InvokeVoidAsync("blazor_NavBar_AfterRender");
        }

        public async Task Setup() // call on Layout After First Render After Init
        {
            //var navLinks = ComponentsCache.Components.Where(c => c.Value.GetType() == typeof(MyNavLink)).ToDictionary(k => k.Key, v => (MyNavLink)v.Value);
            //var navLinkDotNetRefs = navLinks.ToDictionary(k => k.Key, v => DotNetObjectReference.Create(v.Value));
            await (await ModuleAsync).InvokeVoidAsync("blazor_Layout_AfterRender_SetupNavbar").ConfigureAwait(false);
            //await SetControlStateAsync(ComponentState.Enabled, this);
            IsSetupCompleted = true;
        }

        private async Task NavBar_WindowResized()
        {
            if (RunPureJavascriptVersion)
                return;

            await SyncNavBarAnimsCreation.WaitAsync().ConfigureAwait(false);

            await FinishAndRemoveRunningAnimsAsync().ConfigureAwait(false);
            await AdjustNavMenusToDeviceSizeAsync().ConfigureAwait(false);

            SyncNavBarAnimsCreation.Release();
        }

        protected async Task BtnSignUp_ClickAsync()
        {
            await (await ComponentByClassAsync<MyModalBase>("my-login-modal")).HideModalAsync();
            var qs = new Dictionary<string, string> { ["returnUrl"] = NavigationManager.Uri.BeforeFirstOrWhole("?").UTF8ToBase58() }.ToQueryString();
            var registerUrl = PathUtils.Combine(PathSeparator.FSlash, NavigationManager.BaseUri, $"~/Account/Register?{qs}");
            await NavigateAndUpdateActiveNavLinksAsync(registerUrl);

            var jqContentContainer = await JQuery.QueryOneAsync(".my-page-container > .my-page-content > .my-container");
            if ((await jqContentContainer.ClassesAsync()).Contains("disable-css-transition"))
                await jqContentContainer.RemoveClassAsync("disable-css-transition");
        }

        protected async Task BtnResetPassword_ClickAsync()
        {
            await (await ComponentByClassAsync<MyModalBase>("my-login-modal")).HideModalAsync();
            var qs = new Dictionary<string, string> { ["returnUrl"] = NavigationManager.Uri.BeforeFirstOrWhole("?").UTF8ToBase58() }.ToQueryString();
            var forgotPasswordUrl = PathUtils.Combine(PathSeparator.FSlash, NavigationManager.BaseUri, $"~/Account/ForgotPassword?{qs}");
            await NavigateAndUpdateActiveNavLinksAsync(forgotPasswordUrl);

            var jqContentContainer = await JQuery.QueryOneAsync(".my-page-container > .my-page-content > .my-container");
            if ((await jqContentContainer.ClassesAsync()).Contains("disable-css-transition"))
                await jqContentContainer.RemoveClassAsync("disable-css-transition");
        }
        
        protected async Task BtnEdit_ClickAsync()
        {
            await (await ComponentByClassAsync<MyModalBase>("my-login-modal")).HideModalAsync();
            var editurl = PathUtils.Combine(PathSeparator.FSlash, NavigationManager.BaseUri, $"~/Account/Edit");
            await NavigateAndUpdateActiveNavLinksAsync(editurl);

            var jqContentContainer = await JQuery.QueryOneAsync(".my-page-container > .my-page-content > .my-container");
            if ((await jqContentContainer.ClassesAsync()).Contains("disable-css-transition"))
                await jqContentContainer.RemoveClassAsync("disable-css-transition");
        }

        public async Task<StringRange> GetSlideClipPathAsync(bool show, string dropClass, double width, double height)
        {
            var windowWidth = await JQuery.QueryOneAsync("window").WidthAsync().ConfigureAwait(false);

            if (show)
            {
                if (dropClass.EndsWithInvariant("dropdown") || windowWidth < 768)
                    return new StringRange($"polygon(0 0, {width}px 0, {width}px 0, 0 0)", $"polygon(0 0, {width}px 0, {width}px {height}px, 0 {height}px)");
                if (dropClass.EndsWithInvariant("dropright"))
                    return new StringRange($"polygon(0 0, 0 0, 0 {height}px, 0 {height}px)", $"polygon(0 0, {width}px 0, {width}px {height}px, 0 {height}px)");
                if (dropClass.EndsWithInvariant("dropleft"))
                    return new StringRange($"polygon({width}px 0, {width}px 0, {width}px {height}px, {width}px {height}px)", $"polygon(0.00000001px 0, {width}px 0, {width}px {height}px, 0.00000001px {height}px)");
                if (dropClass.EndsWithInvariant("dropup"))
                    return new StringRange($"polygon(0 {height}px, {width}px {height}px, {width}px {height}px, 0 {height}px)", $"polygon(0 0.00000001px, {width}px 0.00000001px, {width}px {height}px, 0 {height}px)");
            }
            else
            {
                if (dropClass.EndsWithInvariant("dropdown") || windowWidth < 768)
                    return new StringRange($"polygon(0 0, {width}px 0, {width}px {height}px, 0 {height}px)", $"polygon(0 0, {width}px 0, {width}px 0.00000001px, 0 0.00000001px)");
                if (dropClass.EndsWithInvariant("dropright"))
                    return new StringRange($"polygon(0 0, {width}px 0, {width}px {height}px, 0 {height}px)", $"polygon(0 0, 0.00000001px 0, 0.00000001px {height}px, 0 {height}px)");
                if (dropClass.EndsWithInvariant("dropleft"))
                    return new StringRange($"polygon(0.00000001px 0, {width}px 0, {width}px {height}px, 0.00000001px {height}px)", $"polygon({width}px 0, {width}px 0, {width}px {height}px, {width}px {height}px)");
                if (dropClass.EndsWithInvariant("dropup"))
                    return new StringRange($"polygon(0 0.00000001px, {width}px 0.00000001px, {width}px {height}px, 0 {height}px)", $"polygon(0 {height}px, {width}px {height}px, {width}px {height}px, 0 {height}px)");
            }

            throw new InvalidEnumArgumentException("invalid class it should not happen");
        }

        public async Task PrepareNavMenuAsync(JQuery navLink, string dropClass)
        {
            Debug.Print(nameof(PrepareNavMenuAsync) + "(): Starting");
            navLink = navLink ?? throw new NullReferenceException(nameof(navLink));

            var navItem = await navLink.ClosestAsync(".my-nav-item").ConfigureAwait(false);
            var navMenu = await navItem.ChildrenAsync(".my-nav-menu").FirstAsync().ConfigureAwait(false);
            var navMenuAncestors = await navItem.ParentsUntilAsync(".my-navbar").FilterAsync(".my-nav-menu").ConfigureAwait(false);
            var topMostNavItem = await navMenu.ClosestAsync(".my-navbar > .my-nav-item").ConfigureAwait(false);

            var removedClass = await navMenu.RemoveClassAndGetRemovedAsync("my-d-none").ConfigureAwait(false);
            var addedClass = await navMenu.AddClassAndGetAddedAsync("my-d-block").ConfigureAwait(false);

            if ((await navMenu.AttrAsync("anim-height").ConfigureAwait(false)).IsNullOrWhiteSpace())
                await navMenu.AttrAsync("anim-height", await navMenu.OuterHeightAsync().ConfigureAwait(false) + "px").ConfigureAwait(false);
            if ((await navMenu.AttrAsync("anim-width").ConfigureAwait(false)).IsNullOrWhiteSpace())
            {
                if (navMenuAncestors.Count > 0)
                {
                    await navMenu.AttrAsync("anim-width", (await JQuery.QueryOneAsync("window").WidthAsync().ConfigureAwait(false) < 768
                        ? await navMenuAncestors.First().OuterWidthAsync().ConfigureAwait(false)
                        : dropClass.EndsWithAny("dropdown", "dropup")
                            ? Math.Max(
                                  await navMenu.OuterWidthAsync().ConfigureAwait(false),
                                  await navMenuAncestors.First().OuterWidthAsync().ConfigureAwait(false))
                            : await navMenu.OuterWidthAsync().ConfigureAwait(false)) + "px").ConfigureAwait(false);
                }
                else
                {
                    await navMenu.AttrAsync("anim-width", Math.Max(
                        await navMenu.OuterWidthAsync().ConfigureAwait(false),
                        await navLink.OuterWidthAsync().ConfigureAwait(false)) + "px").ConfigureAwait(false);
                }
            }

            if ((await topMostNavItem.AttrAsync("init-height").ConfigureAwait(false)).IsNullOrWhiteSpace())
                await topMostNavItem.AttrAsync("init-height", await topMostNavItem.OuterHeightAsync().ConfigureAwait(false) + "px").ConfigureAwait(false);

            if (dropClass.EndsWithInvariant("dropdown") || await JQuery.QueryOneAsync("window").WidthAsync().ConfigureAwait(false) < 768)
            {
                double left = 0;
                var top = await navLink.OuterHeightAsync().ConfigureAwait(false);
                var minWidth = "100%";

                if (navMenuAncestors.Count > 0)
                {
                    var navMenuParent = navMenuAncestors.First();
                    var navMenuParentBorderWidth = await navMenuParent.CssAsync("border-width").ToDoubleAsync().ConfigureAwait(false);
                    var navMenuParentLeftPadding = await navMenuParent.CssAsync("padding-left").ToDoubleAsync().ConfigureAwait(false);

                    left = -navMenuParentBorderWidth - navMenuParentLeftPadding;
                    minWidth = (await navMenuParent.OuterWidthAsync().ConfigureAwait(false)) + "px";
                }

                await navMenu.CssAsync(new Dictionary<string, string>
                {
                    ["min-width"] = minWidth,
                    ["left"] = left + "px",
                    ["top"] = top + "px"
                }).ConfigureAwait(false);
            }
            else if (dropClass.EndsWithInvariant("dropright"))
            {
                var navLinkBorderLeftWidth = await navItem.ChildrenAsync(".my-nav-link").FirstAsync().CssAsync("border-left-width").ToDoubleAsync().ConfigureAwait(false);
                var navLinkWidth = await navLink.OuterWidthAsync().ConfigureAwait(false);
                var left = navLinkWidth - navLinkBorderLeftWidth;
                double top = 0;

                if (navMenuAncestors.Count > 0)
                {
                    var navMenuParent = navMenuAncestors.First();
                    var navMenuParentWidth = await navMenuParent.OuterWidthAsync().ConfigureAwait(false);
                    var navMenuParentBorderWidth = await navMenuParent.CssAsync("border-width").ToDoubleAsync().ConfigureAwait(false);
                    var navMenuParentLeftPadding = await navMenuParent.CssAsync("padding-left").ToDoubleAsync().ConfigureAwait(false);
                    var navMenuParentTopPadding = await navMenuParent.CssAsync("padding-top").ToDoubleAsync().ConfigureAwait(false);

                    left = navMenuParentWidth - navMenuParentBorderWidth * 2 - navMenuParentLeftPadding;
                    top = -navMenuParentTopPadding - navMenuParentBorderWidth;
                }

                await navMenu.CssAsync(new Dictionary<string, string>
                {
                    ["min-width"] = "0",
                    ["left"] = left + "px",
                    ["top"] = top + "px"
                }).ConfigureAwait(false);
            }
            else if (dropClass.EndsWithInvariant("dropleft"))
            {
                var navLinkBorderLeftWidth = await navItem.ChildrenAsync(".my-nav-link").FirstAsync().CssAsync("border-left-width").ToDoubleAsync().ConfigureAwait(false);
                var navMenuWidth = await navMenu.AttrAsync("anim-width").ToDoubleAsync().ConfigureAwait(false);

                var left = -navMenuWidth + navLinkBorderLeftWidth;
                double top = 0;

                if (navMenuAncestors.Count > 0)
                {
                    var navMenuParent = navMenuAncestors.First();
                    var navMenuParentBorderWidth = await navMenuParent.CssAsync("border-width").ToDoubleAsync().ConfigureAwait(false);
                    var navMenuParentLeftPadding = await navMenuParent.CssAsync("padding-left").ToDoubleAsync().ConfigureAwait(false);
                    var navMenuParentTopPadding = await navMenuParent.CssAsync("padding-top").ToDoubleAsync().ConfigureAwait(false);

                    left = -navMenuWidth - navMenuParentLeftPadding;
                    top = -navMenuParentTopPadding - navMenuParentBorderWidth;
                }

                await navMenu.CssAsync(new Dictionary<string, string>
                {
                    ["min-width"] = "0",
                    ["left"] = left + "px",
                    ["top"] = top + "px"
                }).ConfigureAwait(false);
            }
            else if (dropClass.EndsWithInvariant("dropup"))
            {
                var navMenuHeight = await navMenu.AttrAsync("anim-height").ToDoubleAsync().ConfigureAwait(false);

                double left = 0;
                var top = -navMenuHeight;
                var minWidth = "100%";

                if (navMenuAncestors.Count > 0)
                {
                    var navMenuParent = navMenuAncestors.First();
                    var navMenuParentBorderWidth = await navMenuParent.CssAsync("border-width").ToDoubleAsync().ConfigureAwait(false);
                    var navMenuParentLeftPadding = await navMenuParent.CssAsync("padding-left").ToDoubleAsync().ConfigureAwait(false);

                    left = -navMenuParentBorderWidth - navMenuParentLeftPadding;
                    minWidth = await navMenuParent.OuterWidthAsync().ConfigureAwait(false) + "px";
                }

                await navMenu.CssAsync(new Dictionary<string, string>
                {
                    ["min-width"] = minWidth,
                    ["left"] = left + "px",
                    ["top"] = top + "px"
                }).ConfigureAwait(false);
            }

            if ((await navMenu.AttrAsync("init-offset-top").ConfigureAwait(false)).IsNullOrWhiteSpace())
                await navMenu.AttrAsync("init-offset-top", await navMenu.CssAsync("top").ConfigureAwait(false)).ConfigureAwait(false);

            await navMenu.RemoveClassAsync(addedClass).ConfigureAwait(false);
            await navMenu.AddClassAsync(removedClass).ConfigureAwait(false);

            Debug.Print(nameof(PrepareNavMenuAsync) + "(): Finishing");
        }

        public async Task FinishAndRemoveRunningAnimsAsync()
        {
            Debug.Print(nameof(FinishAndRemoveRunningAnimsAsync) + "(): Starting");
            foreach (var animeJs in NavBarAnims)
            {
                if (animeJs is TimelineJs timeline)
                    foreach (var anim in timeline.Animations)
                        await anim.SeekAsync(anim.Duration).ConfigureAwait(false);
                else if (animeJs is AnimationJs anim)
                    await anim.SeekAsync(anim.Duration).ConfigureAwait(false);
            }

            SpinWait.SpinUntil(() =>
            {
                return NavBarAnims.All(a => a.CompleteCallbackFinished);
            }); // if an anim wasnt even created yet then it will bug
            NavBarAnims.Clear();
            Debug.Print(nameof(FinishAndRemoveRunningAnimsAsync) + "(): Finishing");
        }

        public async Task CreateToggleNmAnimAsync(JQuery navMenu, bool show, string dropClass)
        {
            Debug.Print(nameof(CreateToggleNmAnimAsync) + "(): Starting");
            if (navMenu == null)
                throw new NullReferenceException(nameof(navMenu));

            var height = await navMenu.AttrAsync("anim-height").ToDoubleAsync().ConfigureAwait(false);
            var width = await navMenu.AttrAsync("anim-width").ToDoubleAsync().ConfigureAwait(false);
            var clipPath = await GetSlideClipPathAsync(show, dropClass, width, height).ConfigureAwait(false);

            NavBarAnims.Add(show
                ? await AnimeJsService.CreateAsync(new AnimationJs
                {
                    Targets = { navMenu },
                    ClipPath = clipPath,
                    Opacity = new DoubleRange(0, 1),
                    Duration = TimeSpan.FromMilliseconds(500),
                    Easing = EasingType.EaseOutExpo,
                    Autoplay = false,
                    BeginAsync = Nm_ShowBeginAsync,
                    CompleteAsync = Nm_ShowCompleteAsync
                }).ConfigureAwait(false) : await AnimeJsService.CreateAsync(new AnimationJs
                {
                    Targets = { navMenu },
                    ClipPath = clipPath,
                    Opacity = new DoubleRange(1, 0),
                    Duration = TimeSpan.FromMilliseconds(500),
                    Easing = EasingType.EaseOutCirc,
                    Autoplay = false,
                    CompleteAsync = Nm_HideCompleteAsync
                }).ConfigureAwait(false));

            var windowWidth = await JQuery.QueryOneAsync("window").WidthAsync().ConfigureAwait(false);

            if (windowWidth < 768)
            {
                var topMostNavItem = await navMenu.ClosestAsync(".my-navbar > .my-nav-item").ConfigureAwait(false);
                var parentNavMenu = await navMenu.ParentAsync().ClosestAsync(".my-nav-menu").ConfigureAwait(false);
                var navMenuHeight = await navMenu.AttrAsync("anim-height").ToDoubleAsync().ConfigureAwait(false);
                var ancestorNavMenusPartialHeights = await (await navMenu.ParentsUntilAsync(".my-navbar").FilterAsync(".my-nav-item").ConfigureAwait(false)).SelectAsync(async ni =>
                {
                    if (!await ni.ParentAsync().IsAsync(".my-nav-menu").ConfigureAwait(false))
                        return 0;
                    var nm = await ni.ParentAsync().ConfigureAwait(false);
                    var prevNavItems = await ni.PrevAllAsync(".my-nav-item").ConfigureAwait(false);
                    return
                        await prevNavItems.Prepend(ni).ToAsyncEnumerable().SelectAwait(async nit => await nit.OuterHeightAsync().ConfigureAwait(false)).SumAsync().ConfigureAwait(false)
                        + await nm.CssAsync("padding-top").ToDoubleAsync().ConfigureAwait(false)
                        + await nm.CssAsync("padding-bottom").ToDoubleAsync().ConfigureAwait(false);
                }).ToArrayAsync().ConfigureAwait(false);

                var topMostNavItemHeight = await topMostNavItem.AttrAsync("init-height").ToDoubleAsync().ConfigureAwait(false);
                var parentNavMenuHeight = parentNavMenu != null ? await parentNavMenu.AttrAsync("anim-height").ToDoubleAsync().ConfigureAwait(false) : 0;

                var showHeight = navMenuHeight + ancestorNavMenusPartialHeights.Sum() + topMostNavItemHeight + "px";
                var hideHeight = parentNavMenuHeight + ancestorNavMenusPartialHeights.Skip(1).Sum() + topMostNavItemHeight + "px";

                NavBarAnims.Add(show
                    ? await AnimeJsService.CreateAsync(new AnimationJs
                    {
                        Targets = { topMostNavItem },
                        Height = new StringRange(hideHeight, showHeight),
                        Duration = TimeSpan.FromMilliseconds(500),
                        Easing = EasingType.EaseOutExpo,
                        Autoplay = false
                    }).ConfigureAwait(false)
                    : await AnimeJsService.CreateAsync(new AnimationJs
                    {
                        Targets = { topMostNavItem },
                        Height = new StringRange(showHeight, hideHeight),
                        Duration = TimeSpan.FromMilliseconds(500),
                        Easing = EasingType.EaseOutCirc,
                        Autoplay = false
                    }).ConfigureAwait(false));
            }
            Debug.Print(nameof(CreateToggleNmAnimAsync) + "(): Finishing");
        }

        [JSInvokable]
        public async Task Nm_ShowBeginAsync(Guid animGuid)
        {
            Debug.Print(nameof(Nm_ShowBeginAsync) + $"(animGuid: {animGuid}): Starting");

            var anim = AnimeJsService.Animations.Single(a => a.Guid == animGuid);
            var navMenu = anim.Targets.Single();

            await navMenu.RemoveClassAsync("my-d-none").AddClassAsync("my-d-block").ConfigureAwait(false);

            Debug.Print(nameof(Nm_ShowBeginAsync) + $"(animGuid: {animGuid}): Finishing");
        }

        [JSInvokable]
        public async Task Nm_ShowCompleteAsync(Guid animGuid)
        {
            Debug.Print(nameof(Nm_ShowCompleteAsync) + $"(animGuid: {animGuid}): Starting");

            var anim = AnimeJsService.Animations.Single(a => a.Guid == animGuid);
            var navMenu = anim.Targets.Single();

            if (!await anim.BeganAsync().ConfigureAwait(false))
                await navMenu.RemoveClassAsync("my-d-none").AddClassAsync("my-d-block").ConfigureAwait(false);
            await navMenu.RemoveCssAsync("clip-path").ConfigureAwait(false);

            anim.CompleteCallbackFinished = true;

            Debug.Print(nameof(Nm_ShowCompleteAsync) + $"(animGuid: {animGuid}): Finishing");
        }

        [JSInvokable]
        public async Task Nm_HideCompleteAsync(Guid animGuid)
        {
            Debug.Print(nameof(Nm_HideCompleteAsync) + $"(animGuid: {animGuid}): Starting");

            var anim = AnimeJsService.Animations.Single(a => a.Guid == animGuid);
            var navMenu = anim.Targets.Single();

            await navMenu.RemoveClassAsync("my-d-block").AddClassAsync("my-d-none").ConfigureAwait(false);
            await navMenu.RemoveCssAsync("clip-path").ConfigureAwait(false);

            anim.CompleteCallbackFinished = true;

            Debug.Print(nameof(Nm_HideCompleteAsync) + $"(animGuid: {animGuid}): Finishing");

        }

        public async Task CreateHideOnmAnimAsync(JQueryCollection arrOtherNavMenusToHide)
        {
            if (arrOtherNavMenusToHide == null)
                throw new NullReferenceException(nameof(arrOtherNavMenusToHide));

            Debug.Print(nameof(CreateHideOnmAnimAsync) + $"(arrOtherNavMenusToHide: {arrOtherNavMenusToHide.Count}): Starting");

            var windowWidth = await JQuery.QueryOneAsync("window").WidthAsync().ConfigureAwait(false);

            foreach (var onm in arrOtherNavMenusToHide)
            {
                var height = await onm.AttrAsync("anim-height").ToDoubleAsync().ConfigureAwait(false);
                var width = await onm.AttrAsync("anim-width").ToDoubleAsync().ConfigureAwait(false);
                var dropClass = (await onm.ClosestAsync(".my-nav-item").ClassesAsync().ConfigureAwait(false)).Single(c => c.StartsWithInvariant("my-drop"));

                NavBarAnims.Add(await AnimeJsService.CreateAsync(new AnimationJs
                {
                    Targets = { onm },
                    ClipPath = await GetSlideClipPathAsync(false, dropClass, width, height).ConfigureAwait(false),
                    Opacity = new DoubleRange(1, 0),
                    Duration = TimeSpan.FromMilliseconds(500),
                    Easing = EasingType.EaseOutCirc,
                    Autoplay = false,
                    CompleteAsync = Onm_HideCompleteAsync
                }).ConfigureAwait(false));

                if (windowWidth < 768)
                {
                    var topMostNavItem = await onm.ClosestAsync(".my-navbar > .my-nav-item").ConfigureAwait(false);
                    var parentNavMenu = await onm.ParentAsync().ClosestAsync(".my-nav-menu").ConfigureAwait(false);
                    var navMenuHeight = await onm.AttrAsync("anim-height").ToDoubleAsync().ConfigureAwait(false);
                    var ancestorNavMenusPartialHeights = await (await onm.ParentsUntilAsync(".my-navbar").FilterAsync(".my-nav-item").ConfigureAwait(false)).SelectAsync(async ni =>
                    {
                        if (!await ni.ParentAsync().IsAsync(".my-nav-menu").ConfigureAwait(false))
                            return 0;
                        var nm = await ni.ParentAsync().ConfigureAwait(false);
                        var prevNavItems = await ni.PrevAllAsync(".my-nav-item").ConfigureAwait(false);
                        return
                            await prevNavItems.Prepend(ni).ToAsyncEnumerable().SelectAwait(async nit => await nit.OuterHeightAsync().ConfigureAwait(false)).SumAsync().ConfigureAwait(false)
                            + await nm.CssAsync("padding-top").ToDoubleAsync().ConfigureAwait(false)
                            + await nm.CssAsync("padding-bottom").ToDoubleAsync().ConfigureAwait(false);
                    }).ToArrayAsync().ConfigureAwait(false);

                    var topMostNavItemHeight = await topMostNavItem.AttrAsync("init-height").ToDoubleAsync().ConfigureAwait(false);
                    var parentNavMenuHeight = parentNavMenu != null ? await parentNavMenu.AttrAsync("anim-height").ToDoubleAsync().ConfigureAwait(false) : 0;

                    var showHeight = navMenuHeight + ancestorNavMenusPartialHeights.Sum() + topMostNavItemHeight + "px";
                    var hideHeight = parentNavMenuHeight + ancestorNavMenusPartialHeights.Skip(1).Sum() + topMostNavItemHeight + "px";

                    NavBarAnims.Add(await AnimeJsService.CreateAsync(new AnimationJs
                    {
                        Targets = { topMostNavItem },
                        Height = new StringRange(showHeight, hideHeight),
                        Duration = TimeSpan.FromMilliseconds(500),
                        Easing = EasingType.EaseOutCirc,
                        Autoplay = false
                    }).ConfigureAwait(false));
                }
            }

            Debug.Print(nameof(CreateHideOnmAnimAsync) + $"(arrOtherNavMenusToHide: {arrOtherNavMenusToHide.Count}): Finishing");
        }

        [JSInvokable]
        public async Task Onm_HideCompleteAsync(Guid animGuid)
        {
            Debug.Print(nameof(Onm_HideCompleteAsync) + $"(animGuid: {animGuid}): Starting");

            var anim = AnimeJsService.Animations.Single(a => a.Guid == animGuid);
            var onm = anim.Targets.Single();
            await onm.RemoveClassAsync("my-d-block").AddClassAsync("my-d-none").ConfigureAwait(false);
            await onm.RemoveCssAsync("clip-path").ConfigureAwait(false);

            anim.CompleteCallbackFinished = true;

            Debug.Print(nameof(Onm_HideCompleteAsync) + $"(animGuid: {animGuid}): Finishing");
        }

        public async Task CreateToggleNmOcIconAnimAsync(JQuery navLink, bool show)
        {
            if (navLink == null)
                throw new NullReferenceException(nameof(navLink));

            Debug.Print(nameof(CreateToggleNmOcIconAnimAsync) + $"(navLink: {navLink.Guid}, show: {show}): Starting");

            var windowWidth = await JQuery.QueryOneAsync("window").WidthAsync().ConfigureAwait(false);
            var openIcon = windowWidth < 768 ? await navLink.FindAsync(".my-nav-link-open-icon-xs").FirstAsync().ConfigureAwait(false) : await navLink.FindAsync(".my-nav-link-open-icon").FirstAsync().ConfigureAwait(false);
            var closeIcon = windowWidth < 768 ? await navLink.FindAsync(".my-nav-link-close-icon-xs").FirstAsync().ConfigureAwait(false) : await navLink.FindAsync(".my-nav-link-close-icon").FirstAsync().ConfigureAwait(false);
            var navLinkContent = await navLink.FindAsync(".my-nav-link-content").FirstAsync().ConfigureAwait(false);

            var iconToHide = show ? openIcon : closeIcon;
            var iconToShow = show ? closeIcon : openIcon;

            NavBarAnims.Add(await AnimeJsService.CreateAsync(new TimelineJs
            {
                Duration = TimeSpan.FromMilliseconds(500),
                Animations =
                {
                    new AnimationJs
                    {
                        Targets = { iconToHide },
                        Opacity = new DoubleRange(1, 0),
                        Duration = TimeSpan.FromMilliseconds(250),
                        Easing = EasingType.EaseInOutSine,
                        Autoplay = false,
                        BeginAsync = NmOcIcon_HideBeginAsync,
                        CompleteAsync = NmOcIcon_HideCompleteAsync
                    },
                    new AnimationJs
                    {
                        Targets = { iconToShow },
                        Opacity = new DoubleRange(0, 1),
                        Duration = TimeSpan.FromMilliseconds(250),
                        Easing = EasingType.EaseInOutSine,
                        Autoplay = false,
                        BeginAsync = NmOcIcon_ShowBeginAsync,
                        CompleteAsync = NmOcIcon_ShowCompleteAsync
                    }
                }
            }).ConfigureAwait(false));

            Debug.Print(nameof(CreateToggleNmOcIconAnimAsync) + $"(navLink: {navLink.Guid}, show: {show}): Finishing");
        }

        [JSInvokable]
        public async Task NmOcIcon_HideBeginAsync(Guid animGuid)
        {
            Debug.Print(nameof(NmOcIcon_HideBeginAsync) + $"(animGuid: {animGuid}): Starting");

            var iconToHide = AnimeJsService.Animations.Single(a => a.Guid == animGuid).Targets.Single();
            var navLink = await iconToHide.ClosestAsync(".my-nav-link").ConfigureAwait(false);
            var navLinkContent = await navLink.FindAsync(".my-nav-link-content").FirstAsync().ConfigureAwait(false);

            await navLink.CssAsync("width", await navLink.OuterWidthAsync().ConfigureAwait(false) + "px").ConfigureAwait(false);
            await navLinkContent.CssAsync("max-width", await navLinkContent.OuterWidthAsync().ConfigureAwait(false) + "px").ConfigureAwait(false);

            Debug.Print(nameof(NmOcIcon_HideBeginAsync) + $"(animGuid: {animGuid}): Finishing");
        }

        [JSInvokable]
        public async Task NmOcIcon_HideCompleteAsync(Guid animGuid)
        {
            Debug.Print(nameof(NmOcIcon_HideCompleteAsync) + $"(animGuid: {animGuid}): Starting");

            var anim = AnimeJsService.Animations.Single(a => a.Guid == animGuid);
            var iconToHide = anim.Targets.Single();
            var navLink = await iconToHide.ClosestAsync(".my-nav-link").ConfigureAwait(false);
            var navLinkContent = await navLink.FindAsync(".my-nav-link-content").FirstAsync().ConfigureAwait(false);
            
            if (!await anim.BeganAsync().ConfigureAwait(false))
            {
                await navLink.CssAsync("width", await navLink.OuterWidthAsync().ConfigureAwait(false) + "px").ConfigureAwait(false);
                await navLinkContent.CssAsync("max-width", await navLinkContent.OuterWidthAsync().ConfigureAwait(false) + "px").ConfigureAwait(false);
            }

            await iconToHide.RemoveClassAsync("my-d-flex").AddClassAsync("my-d-none").ConfigureAwait(false);

            anim.CompleteCallbackFinished = true;

            Debug.Print(nameof(NmOcIcon_HideCompleteAsync) + $"(animGuid: {animGuid}): Finishing");
        }

        [JSInvokable]
        public async Task NmOcIcon_ShowBeginAsync(Guid animGuid)
        {
            Debug.Print(nameof(NmOcIcon_ShowBeginAsync) + $"(animGuid: {animGuid}): Starting");

            var iconToShow = AnimeJsService.Animations.Single(a => a.Guid == animGuid).Targets.Single();
            await iconToShow.RemoveClassAsync("my-d-none").AddClassAsync("my-d-flex").ConfigureAwait(false);

            Debug.Print(nameof(NmOcIcon_ShowBeginAsync) + $"(animGuid: {animGuid}): Finishing");
        }

        [JSInvokable]
        public async Task NmOcIcon_ShowCompleteAsync(Guid animGuid)
        {
            Debug.Print(nameof(NmOcIcon_ShowCompleteAsync) + $"(animGuid: {animGuid}): Starting");

            var anim = AnimeJsService.Animations.Single(a => a.Guid == animGuid);
            var iconToShow = anim.Targets.Single();
            var navLink = await iconToShow.ClosestAsync(".my-nav-link").ConfigureAwait(false);
            var navLinkContent = await navLink.FindAsync(".my-nav-link-content").FirstAsync().ConfigureAwait(false);
            
            if (!await anim.BeganAsync().ConfigureAwait(false))
                await iconToShow.RemoveClassAsync("my-d-none").AddClassAsync("my-d-flex").ConfigureAwait(false);

            await navLink.RemoveCssAsync("width").ConfigureAwait(false);
            await navLinkContent.RemoveCssAsync("max-width").ConfigureAwait(false);

            anim.CompleteCallbackFinished = true;

            Debug.Print(nameof(NmOcIcon_ShowCompleteAsync) + $"(animGuid: {animGuid}): Finishing");
        }

        public async Task CreateHideOnmOcIconAnimAsync(JQueryCollection arrOtherNavMenusToHide)
        {
            if (arrOtherNavMenusToHide == null)
                throw new NullReferenceException(nameof(arrOtherNavMenusToHide));

            Debug.Print(nameof(CreateHideOnmOcIconAnimAsync) + $"(arrOtherNavMenusToHide: {arrOtherNavMenusToHide.Count}): Starting");

            var windowWidth = await JQuery.QueryOneAsync("window").WidthAsync().ConfigureAwait(false);

            foreach (var onm in arrOtherNavMenusToHide)
            {
                var onmNavItem = await onm.ClosestAsync(".my-nav-item").ConfigureAwait(false);
                var onmNavLink = await onmNavItem.ChildrenAsync(".my-nav-link").FirstAsync().ConfigureAwait(false);
                var onmCloseIcon = windowWidth < 768 ? await onmNavLink.FindAsync(".my-nav-link-close-icon-xs").FirstAsync().ConfigureAwait(false) : await onmNavLink.FindAsync(".my-nav-link-close-icon").FirstAsync().ConfigureAwait(false);
                var onmOpenIcon = windowWidth < 768 ? await onmNavLink.FindAsync(".my-nav-link-open-icon-xs").FirstAsync().ConfigureAwait(false) : await onmNavLink.FindAsync(".my-nav-link-open-icon").FirstAsync().ConfigureAwait(false);

                NavBarAnims.Add(await AnimeJsService.CreateAsync(new TimelineJs
                {
                    Duration = TimeSpan.FromMilliseconds(500),
                    Animations =
                    {
                        new AnimationJs
                        {
                            Targets = { onmCloseIcon },
                            Opacity = new DoubleRange(1, 0),
                            Duration = TimeSpan.FromMilliseconds(250),
                            Easing = EasingType.EaseInOutSine,
                            Autoplay = false,
                            BeginAsync = OnmOcIcon_HideBeginAsync,
                            CompleteAsync = OnmOcIcon_HideCompleteAsync
                        },
                        new AnimationJs
                        {
                            Targets = { onmOpenIcon },
                            Opacity = new DoubleRange(0, 1),
                            Duration = TimeSpan.FromMilliseconds(250),
                            Easing = EasingType.EaseInOutSine,
                            Autoplay = false,
                            BeginAsync = OnmOcIcon_ShowBeginAsync,
                            CompleteAsync = OnmOcIcon_ShowCompleteAsync
                        }
                    }
                }).ConfigureAwait(false));
            }

            Debug.Print(nameof(CreateHideOnmOcIconAnimAsync) + $"(arrOtherNavMenusToHide: {arrOtherNavMenusToHide.Count}): Finishing");
        }

        [JSInvokable]
        public async Task OnmOcIcon_HideBeginAsync(Guid animGuid)
        {
            Debug.Print(nameof(OnmOcIcon_HideBeginAsync) + $"(animGuid: {animGuid}): Starting");

            var onmCloseIcon = AnimeJsService.Animations.Single(a => a.Guid == animGuid).Targets.Single();
            var onmNavLink = await onmCloseIcon.ClosestAsync(".my-nav-link").ConfigureAwait(false);
            var onmNavLinkContent = await onmNavLink.FindAsync(".my-nav-link-content").FirstAsync().ConfigureAwait(false);

            await onmNavLink.CssAsync("width", await onmNavLink.OuterWidthAsync().ConfigureAwait(false) + "px").ConfigureAwait(false);
            await onmNavLinkContent.CssAsync("max-width", await onmNavLinkContent.OuterWidthAsync().ConfigureAwait(false) + "px").ConfigureAwait(false);

            Debug.Print(nameof(OnmOcIcon_HideBeginAsync) + $"(animGuid: {animGuid}): Finishing");
        }

        [JSInvokable]
        public async Task OnmOcIcon_HideCompleteAsync(Guid animGuid)
        {
            Debug.Print(nameof(OnmOcIcon_HideCompleteAsync) + $"(animGuid: {animGuid}): Starting");

            var anim = AnimeJsService.Animations.Single(a => a.Guid == animGuid);
            var onmCloseIcon = anim.Targets.Single();
            var onmNavLink = await onmCloseIcon.ClosestAsync(".my-nav-link").ConfigureAwait(false);
            var onmNavLinkContent = await onmNavLink.FindAsync(".my-nav-link-content").FirstAsync().ConfigureAwait(false);
            
            if (!await anim.BeganAsync().ConfigureAwait(false))
            {
                await onmNavLink.CssAsync("width", await onmNavLink.OuterWidthAsync().ConfigureAwait(false) + "px").ConfigureAwait(false);
                await onmNavLinkContent.CssAsync("max-width", await onmNavLinkContent.OuterWidthAsync().ConfigureAwait(false) + "px").ConfigureAwait(false);
            }

            await onmCloseIcon.RemoveClassAsync("my-d-flex").AddClassAsync("my-d-none").ConfigureAwait(false);

            anim.CompleteCallbackFinished = true;

            Debug.Print(nameof(OnmOcIcon_HideCompleteAsync) + $"(animGuid: {animGuid}): Finishing");
        }

        [JSInvokable]
        public async Task OnmOcIcon_ShowBeginAsync(Guid animGuid)
        {
            Debug.Print(nameof(OnmOcIcon_ShowBeginAsync) + $"(animGuid: {animGuid}): Starting");

            var onmOpenIcon = AnimeJsService.Animations.Single(a => a.Guid == animGuid).Targets.Single();
            await onmOpenIcon.RemoveClassAsync("my-d-none").AddClassAsync("my-d-flex").ConfigureAwait(false);

            Debug.Print(nameof(OnmOcIcon_ShowBeginAsync) + $"(animGuid: {animGuid}): Finishing");
        }

        [JSInvokable]
        public async Task OnmOcIcon_ShowCompleteAsync(Guid animGuid)
        {
            Debug.Print(nameof(OnmOcIcon_ShowCompleteAsync) + $"(animGuid: {animGuid}): Starting");

            var anim = AnimeJsService.Animations.Single(a => a.Guid == animGuid);
            var onmOpenIcon = anim.Targets.Single();
            var onmNavLink = await onmOpenIcon.ClosestAsync(".my-nav-link").ConfigureAwait(false);
            var onmNavLinkContent = await onmNavLink.FindAsync(".my-nav-link-content").FirstAsync().ConfigureAwait(false);

            if (!await anim.BeganAsync().ConfigureAwait(false))
                await onmOpenIcon.RemoveClassAsync("my-d-none").AddClassAsync("my-d-flex").ConfigureAwait(false);

            await onmNavLink.RemoveCssAsync("width").ConfigureAwait(false);
            await onmNavLinkContent.RemoveCssAsync("max-width").ConfigureAwait(false);

            anim.CompleteCallbackFinished = true;

            Debug.Print(nameof(OnmOcIcon_ShowCompleteAsync) + $"(animGuid: {animGuid}): Finishing");
        }

        public async Task AdjustNavMenusToDeviceSizeAsync()
        {
            Debug.Print(nameof(AdjustNavMenusToDeviceSizeAsync) + "(): Starting");

            var windowWidth = await JQuery.QueryOneAsync("window").WidthAsync().ConfigureAwait(false);
            var navMenus = await JQuery.QueryOneAsync(".my-navbar").FindAsync(".my-nav-menu").ConfigureAwait(false);

            foreach (var nm in navMenus)
            {
                var nmNavItem = await nm.ClosestAsync(".my-nav-item").ConfigureAwait(false);
                var nmNavLink = await nmNavItem.ChildrenAsync(".my-nav-link").FirstAsync().ConfigureAwait(false);
                var nmCloseIcon = await nmNavLink.FindAsync(".my-nav-link-close-icon").FirstAsync().ConfigureAwait(false);
                var nmOpenIcon = await nmNavLink.FindAsync(".my-nav-link-open-icon").FirstAsync().ConfigureAwait(false);
                var nmCloseIconXs = await nmNavLink.FindAsync(".my-nav-link-close-icon-xs").FirstAsync().ConfigureAwait(false);
                var nmOpenIconXs = await nmNavLink.FindAsync(".my-nav-link-open-icon-xs").FirstAsync().ConfigureAwait(false);
                var nmChildNavLinks = await nm.ChildrenAsync(".my-nav-item").ChildrenAsync(".my-nav-link").ConfigureAwait(false);
                var menuContainsAtLeastOneIcon = (await nmChildNavLinks.ChildrenAsync(".my-nav-link-icon").ConfigureAwait(false)).Count > 0;
                var topMostNavItem = await nm.ClosestAsync(".my-navbar > .my-nav-item").ConfigureAwait(false);

                await nm.RemoveClassesAsync(new[] { "shown", "my-d-block" }).AddClassAsync("my-d-none").ConfigureAwait(false); // hide all menus

                await nm.RemoveAttrAsync("anim-height").ConfigureAwait(false);  // clear anim-height/width
                await nm.RemoveAttrAsync("anim-width").ConfigureAwait(false);
                await nm.RemoveAttrAsync("init-offset-top").ConfigureAwait(false);
                await topMostNavItem.RemoveAttrAsync("init-height").ConfigureAwait(false);
                await topMostNavItem.RemoveAttrAsync("init-offset-top").ConfigureAwait(false);

                await topMostNavItem.RemoveCssAsync("height").ConfigureAwait(false);

                if (windowWidth < 768)
                {
                    await nmCloseIcon.CssAsync("opacity", 0.ToStringInvariant()).RemoveClassAsync("my-d-flex").AddClassAsync("my-d-none").ConfigureAwait(false);
                    await nmOpenIcon.CssAsync("opacity", 0.ToStringInvariant()).RemoveClassAsync("my-d-flex").AddClassAsync("my-d-none").ConfigureAwait(false);
                    await nmCloseIconXs.CssAsync("opacity", 0.ToStringInvariant()).RemoveClassAsync("my-d-flex").AddClassAsync("my-d-none").ConfigureAwait(false);
                    await nmOpenIconXs.CssAsync("opacity", 1.ToStringInvariant()).RemoveClassAsync("my-d-none").AddClassAsync("my-d-flex").ConfigureAwait(false);

                    await nm.CssAsync("max-width", "100%").ConfigureAwait(false); // max-width to 100% for xs
                }
                else
                {
                    await nmCloseIconXs.CssAsync("opacity", 0.ToStringInvariant()).RemoveClassAsync("my-d-flex").AddClassAsync("my-d-none").ConfigureAwait(false);
                    await nmOpenIconXs.CssAsync("opacity", 0.ToStringInvariant()).RemoveClassAsync("my-d-flex").AddClassAsync("my-d-none").ConfigureAwait(false);
                    await nmCloseIcon.CssAsync("opacity", 0.ToStringInvariant()).RemoveClassAsync("my-d-flex").AddClassAsync("my-d-none").ConfigureAwait(false);
                    await nmOpenIcon.CssAsync("opacity", 1.ToStringInvariant()).RemoveClassAsync("my-d-none").AddClassAsync("my-d-flex").ConfigureAwait(false);
                    await nm.RemoveCssAsync("max-width").ConfigureAwait(false); // disable max width (it is restored during animation setup in positionMenus())
                }

                if (menuContainsAtLeastOneIcon)
                { // add left margin to nav-link content to account for icons length and padding in other nav-links
                    var arrNavLinkContentsWoIcons = await nmChildNavLinks.WhereAsync(async cnv => (await cnv.ChildrenAsync(".my-nav-link-icon").ConfigureAwait(false)).Count == 0)
                        .SelectAsync(async cnv => await cnv.ChildrenAsync(".my-nav-link-content").FirstAsync().ConfigureAwait(false)).ConfigureAwait(false);
                    var icon = (await nmChildNavLinks.WhereAsync(async cnv => (await cnv.ChildrenAsync(".my-nav-link-icon").ConfigureAwait(false)).Count == 1)
                        .SelectAsync(async cnv => await cnv.ChildrenAsync(".my-nav-link-icon").FirstAsync().ConfigureAwait(false)).ConfigureAwait(false)).First();
                    var iconWidth = await icon.OuterWidthAsync().ConfigureAwait(false);

                    foreach (var navLinkContentWoIcon in arrNavLinkContentsWoIcons)
                        await navLinkContentWoIcon.CssAsync("margin-left", iconWidth + "px").ConfigureAwait(false);
                }
            }

            var navBars = await JQuery.QueryAsync(".my-navbar").ConfigureAwait(false);

            foreach (var nb in navBars)
            {
                var nbChildNavLinks = await nb.ChildrenAsync(".my-nav-item").ChildrenAsync(".my-nav-link").ConfigureAwait(false);
                var navBarContainsAtLeastOneIcon = (await nbChildNavLinks.ChildrenAsync(".my-nav-link-icon").ConfigureAwait(false)).Count > 0;

                if (navBarContainsAtLeastOneIcon)
                {
                    var arrNavLinkContentsWoIcons = await nbChildNavLinks
                        .WhereAsync(async cnv => (await cnv.ChildrenAsync(".my-nav-link-icon").ConfigureAwait(false)).Count == 0)
                        .SelectAsync(async cnv => await cnv.ChildrenAsync(".my-nav-link-content").FirstAsync().ConfigureAwait(false)).ConfigureAwait(false);
                    var icon = (await nbChildNavLinks
                        .WhereAsync(async cnv => (await cnv.ChildrenAsync(".my-nav-link-icon").ConfigureAwait(false)).Count == 1)
                        .SelectAsync(async cnv => await cnv.ChildrenAsync(".my-nav-link-icon").FirstAsync().ConfigureAwait(false)).ConfigureAwait(false)).First();
                    var iconWidth = await icon.OuterWidthAsync().ConfigureAwait(false);

                    foreach (var navLinkContentWoIcon in arrNavLinkContentsWoIcons)
                        await navLinkContentWoIcon.CssAsync("margin-left", (windowWidth < 768 ? iconWidth + "px" : "") + "px").ConfigureAwait(false);
                }
            }

            Debug.Print(nameof(AdjustNavMenusToDeviceSizeAsync) + "(): Finishing");
        }

        public async Task RunAnimsAsync()
        {
            Debug.Print(nameof(RunAnimsAsync) + "(): Starting");

            foreach (var anim in NavBarAnims)
                await anim.PlayAsync().ConfigureAwait(false);

            Debug.Print(nameof(RunAnimsAsync) + "(): Finishing");
        }

        //public void Dispose()
        //{
        //    Dispose(true);
        //    GC.SuppressFinalize(this);
        //}

        //protected virtual void Dispose(bool disposing)
        //{
        //    if (!disposing)
        //        return;

        //    _syncNavBarAnims.Dispose();
        //}
    }
}
