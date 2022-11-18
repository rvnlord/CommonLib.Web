using System.Threading.Tasks;

namespace CommonLib.Web.Source.Common.Components.MyCardComponent;

public class MyCardHeaderBase : MyComponentBase
{
    protected override async Task OnInitializedAsync()
    {
        await Task.CompletedTask;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (FirstParamSetup)
        {
            SetMainAndUserDefinedClasses("my-card-header");
            SetUserDefinedStyles();
            SetUserDefinedAttributes();
        }
            
        await Task.CompletedTask;
    }
}