using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Components.Interfaces;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Models;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor.Components;

namespace CommonLib.Web.Source.Common.Components.ExtRadialGaugeComponent
{
    public class ExtRadialGaugeBase : MyComponentBase, IValidable
    {
        public TelerikRadialGauge G { get; set; }
        
		[Parameter]
        public RenderFragment RadialGaugeScales { get; set; }

        [Parameter]
        public RenderFragment RadialGaugePointers { get; set; }
		
        [Parameter]
        public BlazorParameter<bool?> Validate { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await Task.CompletedTask;
        }
        
        protected override async Task OnParametersSetAsync()
        {
            if (FirstParamSetup)
            {
                SetMainCustomAndUserDefinedClasses("ext-radialgauge", new [] { $"my-guid_{Guid}" });
                SetUserDefinedStyles();
                SetUserDefinedAttributes();
            }

            if (Validate.HasChanged())
                Validate.ParameterValue ??= true; 

            CascadedEditContext.BindAlwaysValidValidationStateChanged(this);
            
            await Task.CompletedTask;
        }
        
        protected override async Task OnAfterFirstRenderAsync()
        {
            await Task.CompletedTask;
        }
    }
}
