using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Utils.UtilClasses;

namespace CommonLib.Web.Source.Common.Components.MyCardComponent;

public class MyCardBase : MyComponentBase
{
    private MyComponentBase[] _allControls;

    protected override async Task OnInitializedAsync()
    {
        await Task.CompletedTask;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (FirstParamSetup)
        {
            SetMainAndUserDefinedClasses("my-card");
            SetUserDefinedStyles();
            SetUserDefinedAttributes();
        }
            
        await Task.CompletedTask;
    }

    protected override async Task OnAfterFirstRenderAsync()
    {
        _allControls = GetInputControls(); // Make sure to get images if they are ever not part of input controls | Consider removing Interactivity State management from heeree (a component) and handle it in an actual page, i.e.: List of Users

        await SetControlStatesAsync(ComponentState.Enabled, _allControls);
    }
}