using System.Threading.Tasks;

namespace CommonLib.Web.Source.Common.Components.MyCssGridComponent
{
    public class MyCssGridBase : MyComponentBase
    {
        //[Parameter]
        //public BlazorParameter<CssGridLayout> ColumnsLayout { get; set; }
        
        //[Parameter]
        //public BlazorParameter<CssGridLayout> RowsLayout { get; set; }

        //[Parameter]
        //public BlazorParameter<CssPadding> Padding { get; set; }

        //[Parameter]
        //public BlazorParameter<CssGap> Gap { get; set; }

        //[Parameter]
        //public BlazorParameter<CssGap> ColumnsGap { get; set; }

        //[Parameter]
        //public BlazorParameter<CssGap> RowsGap { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await Task.CompletedTask;
        }

        protected override async Task OnParametersSetAsync()
        {
            if (FirstParamSetup)
            {
                SetMainAndUserDefinedClasses("my-css-grid");
                SetUserDefinedStyles();
                SetUserDefinedAttributes();
            }
            
            await Task.CompletedTask;
        }
    }
}
