using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace CommonLib.Web.Source.Common.Components.MyNavBrandComponent
{
    public class MyNavBrandBase : MyComponentBase
    {
        [Parameter]
        public string MainImage { get; set; }

        [Parameter]
        public string AltImage { get; set; }

        protected override async Task OnInitializedAsync() => await Task.CompletedTask;

        protected override async Task OnParametersSetAsync()
        {
            if (FirstParamSetup)
            {
                SetMainAndUserDefinedClasses("my-nav-brand");
            }

            await Task.CompletedTask;
        }
        protected override async Task OnAfterFirstRenderAsync() => await Task.CompletedTask;
    }
}
