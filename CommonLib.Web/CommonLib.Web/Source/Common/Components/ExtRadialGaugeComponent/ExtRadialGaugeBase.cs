using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor.Components;

namespace CommonLib.Web.Source.Common.Components.ExtRadialGaugeComponent
{
    public class ExtRadialGaugeBase : MyComponentBase
    {
        public TelerikRadialGauge G { get; set; }
        
		[Parameter]
        public RenderFragment RadialGaugeScales { get; set; }

        [Parameter]
        public RenderFragment RadialGaugePointers { get; set; }
		
        protected override async Task OnInitializedAsync()
        {
            await Task.CompletedTask;
        }
        
        protected override async Task OnParametersSetAsync()
        {
            if (FirstParamSetup)
            {
                SetMainCustomAndUserDefinedClasses("ext-radialgauge", new [] { $"my-guid_{_guid}" });
                SetUserDefinedStyles();
                SetUserDefinedAttributes();
            }
            
            await Task.CompletedTask;
        }
        
        protected override async Task OnAfterFirstRenderAsync()
        {
            await Task.CompletedTask;
        }
    }
}
