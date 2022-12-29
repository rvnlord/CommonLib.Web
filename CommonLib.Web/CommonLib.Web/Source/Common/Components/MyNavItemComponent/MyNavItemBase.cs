﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Models;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components.MyNavLinkComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Services.Interfaces;
using CommonLib.Web.Source.ViewModels.Account;
using Microsoft.AspNetCore.Components;

namespace CommonLib.Web.Source.Common.Components.MyNavItemComponent
{
    public class MyNavItemBase : MyComponentBase
    {
        //private MyComponentBase[] _navItemAndNavLink => this.ToArrayOfOne().Cast<MyComponentBase>().Concat(Children.OfType<MyNavLinkBase>()).ToArray();
        private MyNavLinkBase _disabledNavLink => Children.OfType<MyNavLinkBase>().SingleOrDefault(nl => nl.InteractionState.V.IsDisabledOrForceDisabled);
        private List<MyComponentBase> _disabledNavLinkContent => _disabledNavLink.Children;
        
        [Parameter]
        public IconType Icon { get; set; }
        
        [Parameter]
        public string To { get; set; }

        [Parameter]
        public bool MatchEmptyRoute { get; set; }

        [Parameter] 
        public NavItemType Type { get; set; } = NavItemType.Link;

        [Parameter] 
        public NavItemPlacement Placement { get; set; } = NavItemPlacement.Left;

        public Search Search { get; set; } = new();

        protected override async Task OnParametersSetAsync()
        {
            if (IsFirstParamSetup())
                SetMainAndUserDefinedClasses("my-nav-item");

            var customClasses = new List<string>();
            if (Type != NavItemType.Link)
                customClasses.Unshift($"my-{Type.EnumToString().Remove("link").ToLowerInvariant()}");
            if (Placement == NavItemPlacement.Right)
                customClasses.Unshift("my-ml-auto");
            AddClasses(customClasses);

            await Task.CompletedTask;
        }
        
        protected override async Task OnAfterRenderAsync(bool _)
        {
            if (Type == NavItemType.Login)
            {
                var prevAuthUser = Mapper.Map(AuthenticatedUser, new AuthenticateUserVM()); // to prevent  
                var authResp = await AccountClient.GetAuthenticatedUserAsync();
                if (!authResp.IsError)
                {
                    AuthenticatedUser = authResp.Result;
                    AuthenticatedUser.Avatar = (await AccountClient.GetUserAvatarByNameAsync(AuthenticatedUser.UserName))?.Result;
                    if (!AuthenticatedUser.Equals(prevAuthUser))
                        await StateHasChangedAsync(true);
                    if (_disabledNavLink is not null && _disabledNavLink.InteractionState.V.IsDisabledOrForceDisabled)
                        await SetControlStateAsync(ComponentState.Enabled, _disabledNavLink);
                }
            }
            else if (_disabledNavLink is not null && _disabledNavLink.InteractionState.V.IsDisabledOrForceDisabled)
                await SetControlStatesAsync(ComponentState.Enabled, _disabledNavLinkContent.Prepend(_disabledNavLink)); // need to set children directly because the way I am reendering nav-links is that I am swapping icons, buttons etc with their disabled/enabled equivalent each time
        }
    }

    public enum NavItemType
    {
        Link,
        DropDown,
        DropUp,
        DropLeft,
        DropRight,
        Brand,
        Input,
        Toggler,
        Login,
        Search,
        Home
    }

    public enum NavItemPlacement
    {
        Left,
        Right
    }
}
