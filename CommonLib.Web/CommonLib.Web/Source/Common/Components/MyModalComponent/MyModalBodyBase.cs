using System.Threading.Tasks;

namespace CommonLib.Web.Source.Common.Components.MyModalComponent
{
    public class MyModalBodyBase : MyComponentBase
    {
        protected override async Task OnParametersSetAsync()
        {
            if (IsFirstParamSetup())
            {
                SetMainAndUserDefinedClasses("my-modal-body");
                SetUserDefinedStyles();
            }
            
            await Task.CompletedTask;
        }

        protected override async Task OnInitializedAsync() => await Task.CompletedTask;
        protected override async Task OnAfterFirstRenderAsync() => await Task.CompletedTask;
    }
}
