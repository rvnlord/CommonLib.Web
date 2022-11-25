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
            if (FirstParamSetup)
            {
                SetMainAndUserDefinedClasses("my-nav-menu");
                SetUserDefinedStyles();
                SetUserDefinedAttributes();
            }

            await Task.CompletedTask;
        }

        protected override async Task OnInitializedAsync() => await Task.CompletedTask;
        protected override async Task OnAfterFirstRenderAsync() => await Task.CompletedTask;
    }
}
