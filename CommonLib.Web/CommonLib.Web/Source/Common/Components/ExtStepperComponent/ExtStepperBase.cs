using Microsoft.AspNetCore.Components;
using Telerik.Blazor.Components;

namespace CommonLib.Web.Source.Common.Components.ExtStepperComponent
{
    public class ExtStepperBase : MyComponentBase
    {
        [Parameter]
        public TelerikStepper Ts { get; set; }

		[Parameter]
		public RenderFragment StepperSteps { get; set; }
    }
}
