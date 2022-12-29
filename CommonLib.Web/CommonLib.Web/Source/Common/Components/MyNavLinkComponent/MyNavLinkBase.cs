using System;
using System.Linq;
using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Components.MyNavBarComponent;
using CommonLib.Web.Source.Common.Components.MyNavItemComponent;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Services.Interfaces;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Threading;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components.MyInputComponent;

namespace CommonLib.Web.Source.Common.Components.MyNavLinkComponent
{
    public class MyNavLinkBase : MyComponentBase
    {
        protected ElementReference _jsNavLink { get; set; }
        protected IconType _openIcon { get; set; }
        protected IconType _closeIcon { get; set; }
        protected IconType _closeIconXs { get; set; }
        protected IconType _openIconXs { get; set; }
        protected IconType _icon { get; set; }

        protected string _absoluteVirtualLink { get; set; }

        [Inject]
        public IJQueryService JQuery { get; set; }

        [CascadingParameter]
        public IconType CascadedIcon { get; set; }

        [Parameter]
        public IconType Icon { get; set; }

        [Parameter]
        public string To { get; set; }
        
        [Parameter]
        public NavLinkIconPlacement IconPlacement { get; set; } = NavLinkIconPlacement.Left;

        [Parameter]
        public object Image { get; set; }
        
        [Parameter]
        public NavLinkImagePlacement ImagePlacement { get; set; } = NavLinkImagePlacement.Left;

        [Parameter]
        public bool MatchEmptyRoute { get; set; }
        
        [CascadingParameter]
        public NavItemType NavItemType { get; set; }
        
        protected override async Task OnInitializedAsync()
        {
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

            await Task.CompletedTask;
        }
        
        protected override async Task OnAfterFirstRenderAsync() // this is executed before outer component after render but the outer component won't wait until this is finished unless forced
        {
            await (await ModuleAsync).InvokeVoidAndCatchCancellationAsync("blazor_NavLink_AfterFirstRender", _guid, DotNetObjectReference.Create(this));
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await JQuery.QueryOneAsync(_guid).AttrAsync("rendered", "true");
            //await (await ModuleAsync).InvokeVoidAndCatchCancellationAsync("blazor_NavLink_AfterRender", _guid);
        }

        [JSInvokable]
        public void NavLink_Click()
        {
            NavigationManager.NavigateTo(_absoluteVirtualLink);
        }

        private async Task CurrentEditContext_ValidationStateChangedAsync(MyEditContext sender, MyValidationStateChangedEventArgs e, CancellationToken _)
        {
            if (e == null)
                throw new NullReferenceException(nameof(e));
            if (e.ValidationMode == ValidationMode.Property && e.ValidatedFields == null)
                throw new NullReferenceException(nameof(e.ValidatedFields));
            if (Ancestors.Any(a => a is MyInputBase))
                return;
            
            if (e.ValidationMode == ValidationMode.Model)
            {
                var state = e.ValidationStatus.In(ValidationStatus.Pending, ValidationStatus.Success) 
                    ? ComponentState.Disabled 
                    : ComponentState.Enabled;
                await SetControlStateAsync(state, this);
            }
        }
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

    public enum NavLinkImagePlacement
    {
        Left,
        Right
    }
}
