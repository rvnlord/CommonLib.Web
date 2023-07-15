using System;
using System.Linq;
using System.Threading.Tasks;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Services.Upload.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.Collections.Extensions;
using Telerik.Blazor.Components;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Extensions.Collections;
using CommonLib.Web.Source.Services.Interfaces;
using System.Drawing;
using CommonLib.Web.Source.Models;
using Microsoft.JSInterop;

namespace CommonLib.Web.Source.Common.Components.ExtStepperComponent
{
    public class ExtStepperBase : MyComponentBase
    {
        private DotNetObjectReference<ExtStepperBase> _extStepperDotNetRef;

        protected MySvgIcon _completedIcon { get; set; }
        protected MySvgIcon _failedIcon { get; set; }
        
        public int CurrentStep { get; set; }
        public OrderedDictionary<int, StepStatus> StepStatuses { get; set; }
        
        public TelerikStepper Ts { get; set; }

        [Parameter]
        public BlazorParameter<bool?> IsInteractive { get; set; }

        [Parameter]
        public RenderFragment StepperSteps { get; set; }

        [Inject]
        public IUploadClient UploadClient { get; set; }

        [Inject]
        public IJQueryService JQuery { get; set; }

        protected override async Task OnInitializedAsync()
        {
            StepStatuses ??= new OrderedDictionary<int, StepStatus>();
            _completedIcon = await MySvgIcon.FromIconTypeAsync(IconType.From(LightIconType.Check), UploadClient);
            _failedIcon = await MySvgIcon.FromIconTypeAsync(IconType.From(LightIconType.Ban), UploadClient);
            await Task.CompletedTask;
        }

        protected override async Task OnParametersSetAsync()
        {
            if (FirstParamSetup)
            {
                SetMainCustomAndUserDefinedClasses("ext-stepper", new[] { $"my-guid_{Guid}" });
                SetUserDefinedStyles();
                SetUserDefinedAttributes();
            }

            if (IsInteractive.HasChanged())
            {
                IsInteractive.ParameterValue ??= true;
                if (IsInteractive.V == true)
                    RemoveClass("noninteractive");
                else
                    AddClass("noninteractive");
            }
            
            await Task.CompletedTask;
        }

        protected override async Task OnAfterFirstRenderAsync()
        {
            _extStepperDotNetRef = DotNetObjectReference.Create(this);
            await (await InputModuleAsync).InvokeVoidAndCatchCancellationAsync("blazor_ExtStepper_AfterFirstRender", Guid, _extStepperDotNetRef).ConfigureAwait(false);
        }

        //[JSInvokable] // Instead of:  @*ValueChanged="Stepper_StepChangedAsync" *@ to account for clicks not always triggering
        public async Task Stepper_StepChangedAsync(int stepIndex)
        {
            await SetStatusAsync(stepIndex, StepStatus.Pending);
        }

        public async Task SetStatusAsync(int stepIndex, StepStatus stepStatus)
        {
            CurrentStep = stepIndex;
            var i = 0;
            while (i <= stepIndex || StepStatuses.ContainsKey(i))
            {
                if (i < stepIndex)
                    StepStatuses.AddOrUpdate(i, StepStatus.Completed);
                else if (i > stepIndex)
                    StepStatuses.AddOrUpdate(i, StepStatus.Pending);
                else if (i == stepIndex)
                    StepStatuses.AddOrUpdate(i, stepStatus);
                i++;
            }
            StateHasChanged(true);
            var stepStatusClass = stepStatus switch
            {
                StepStatus.Pending => "k-step-current",
                StepStatus.Completed => "k-step-done",
                StepStatus.Failed => "my-k-step-failed",
                _ => throw new ArgumentOutOfRangeException(null, "Invalid status")
            };

            await JQuery.QueryOneAsync(Guid).FindAsync($".k-step-list > .k-step:eq({stepIndex})").FirstAsync().AddClassAsync(stepStatusClass);
        }

        public MySvgIcon GetIconWithStatus(int stepIndex, MySvgIcon icon)
        {
            return (StepStatuses.ContainsKey(stepIndex) ? StepStatuses.VorDef(stepIndex) : StepStatus.Pending) switch
            {
                StepStatus.Pending => icon,
                StepStatus.Completed => _completedIcon,
                StepStatus.Failed => _failedIcon,
                _ => throw new ArgumentOutOfRangeException(null, "Invalid status")
            };
        }
    }

    public enum StepStatus
    {
        Pending,
        Completed,
        Failed
    }
}
