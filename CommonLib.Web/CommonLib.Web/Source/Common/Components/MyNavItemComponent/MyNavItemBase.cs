using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Models;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Web.Source.ViewModels.Account;
using Microsoft.AspNetCore.Components;

namespace CommonLib.Web.Source.Common.Components.MyNavItemComponent
{
    public class MyNavItemBase : MyComponentBase
    {
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
                AuthenticatedUser = (await AccountClient.GetAuthenticatedUserAsync()).Result;
                if (!AuthenticatedUser.Equals(prevAuthUser))
                    await StateHasChangedAsync();
            }
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
