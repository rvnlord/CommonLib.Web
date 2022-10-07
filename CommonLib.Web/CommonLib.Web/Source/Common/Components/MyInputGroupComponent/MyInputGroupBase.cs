using System.Threading.Tasks;

namespace CommonLib.Web.Source.Common.Components.MyInputGroupComponent
{
    public class MyInputGroupBase : MyComponentBase
    {
        protected override async Task OnInitializedAsync() => await Task.CompletedTask;
        protected override async Task OnParametersSetAsync()
        {
            if (FirstParamSetup)
            {
                SetMainAndUserDefinedClasses("my-input-group");
                SetUserDefinedStyles();
                SetUserDefinedAttributes();
            }

            await Task.CompletedTask;
        }
        protected override async Task OnAfterFirstRenderAsync() => await Task.CompletedTask;
    }
}
