using System;
using System.Threading.Tasks;
using CommonLib.Source.Common.Extensions.Collections;
using MoreLinq;

namespace CommonLib.Web.Source.Common.Components.MyNavMenuComponent
{
    public class MyNavMenuBase : MyComponentBase
    {
        protected override async Task OnParametersSetAsync()
        {
            //if (IsItFirstParamSetup())
            //{
                //SetMainAndUserDefinedClasses("my-nav-menu", true);
                _renderClasses = (AdditionalAttributes.VorN("class")?.ToString()?.Split(" ") ?? Array.Empty<string>()).Prepend("my-nav-menu").JoinAsString(" ");
                //SetUserDefinedStyles(true); // don't render because it will override jquery set styles for animations
                //SetUserDefinedAttributes(true);
            //}

            await Task.CompletedTask;
        }

        protected override async Task OnInitializedAsync() => await Task.CompletedTask;
        protected override async Task OnAfterFirstRenderAsync() => await Task.CompletedTask;
    }
}
