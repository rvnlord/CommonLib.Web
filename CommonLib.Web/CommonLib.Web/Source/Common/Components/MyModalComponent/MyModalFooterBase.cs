using System.Threading.Tasks;

namespace CommonLib.Web.Source.Common.Components.MyModalComponent
{
    public class MyModalFooterBase : MyComponentBase
    {
        protected override async Task OnParametersSetAsync()
        {
            if (IsFirstParamSetup())
            {
                SetMainAndUserDefinedClasses("my-modal-footer");
                SetUserDefinedStyles();
            }
            
            await Task.CompletedTask;
        }

        protected override async Task OnInitializedAsync() => await Task.CompletedTask;
        protected override async Task OnAfterFirstRenderAsync() => await Task.CompletedTask;
    }
}
