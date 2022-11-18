using System.Threading.Tasks;

namespace CommonLib.Web.Source.Common.Components.MyCardComponent;

public class MyCardBodyBase : MyComponentBase
{
    protected override async Task OnInitializedAsync()
    {
        await Task.CompletedTask;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (FirstParamSetup)
        {
            SetMainAndUserDefinedClasses("my-card-body");
            SetUserDefinedStyles();
            SetUserDefinedAttributes();
        }
            
        await Task.CompletedTask;
    }
}