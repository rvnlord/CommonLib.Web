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
        
        protected string _absoluteVirtualLink { get; set; }

        public IconType IconState { get; set; }

        [Inject]
        public IJQueryService JQuery { get; set; }

        [CascadingParameter(Name = "CascadingIconType")]
        public IconType CascadingIconType { get; set; }

        [CascadingParameter(Name = "CascadingNavItemType")]
        public NavItemType CascadingNavItemType { get; set; }

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

        protected override async Task OnInitializedAsync()
        {
            await Task.CompletedTask;
        }

        protected override async Task OnParametersSetAsync()
        {
            if (FirstParamSetup)
            {
                if (CascadingNavItemType.In(NavItemType.Link, NavItemType.Home))
                    _absoluteVirtualLink = To == null ? null : PathUtils.Combine(PathSeparator.FSlash, NavigationManager.BaseUri, To);
                
                SetMainAndUserDefinedClasses("my-nav-link");
                SetUserDefinedStyles();
                SetUserDefinedAttributes();
                
                _openIcon = CascadingNavItemType switch
                {
                    NavItemType.Link => null,
                    NavItemType.DropDown => IconType.From(LightIconType.ChevronCircleDown),
                    NavItemType.DropUp => IconType.From(LightIconType.ChevronCircleUp),
                    NavItemType.DropLeft => IconType.From(LightIconType.ChevronCircleLeft),
                    NavItemType.DropRight => IconType.From(LightIconType.ChevronCircleRight),
                    _ => null
                };
                _closeIcon = CascadingNavItemType switch
                {
                    NavItemType.Link => null,
                    NavItemType.DropDown => IconType.From(LightIconType.ChevronCircleUp),
                    NavItemType.DropUp => IconType.From(LightIconType.ChevronCircleDown),
                    NavItemType.DropLeft => IconType.From(LightIconType.ChevronCircleRight),
                    NavItemType.DropRight => IconType.From(LightIconType.ChevronCircleLeft),
                    _ => null
                };
                _openIconXs = CascadingNavItemType == NavItemType.Link ? null : IconType.From(LightIconType.ChevronCircleDown);
                _closeIconXs = CascadingNavItemType == NavItemType.Link ? null : IconType.From(LightIconType.ChevronCircleUp);
            }

            if (Icon is not null && Icon != IconState || CascadingIconType is not null && CascadingIconType != IconState)
                IconState = Icon ?? CascadingIconType;

            CascadedEditContext.BindValidationStateChanged(CurrentEditContext_ValidationStateChangedAsync);

            await Task.CompletedTask;
        }
        
        protected override async Task OnAfterFirstRenderAsync() // this is executed before outer component after render but the outer component won't wait until this is finished unless forced
        {
            await (await ModuleAsync).InvokeVoidAndCatchCancellationAsync("blazor_NavLink_AfterFirstRender", Guid, DotNetObjectReference.Create(this));
        }

        protected override async Task OnAfterRenderAsync(bool firstRender, bool authUserChanged)
        {
            await JQuery.QueryOneAsync(Guid).AttrAsync("rendered", "true");
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
