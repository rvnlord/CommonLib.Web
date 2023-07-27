using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components.Interfaces;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Telerik.Blazor.Components;

namespace CommonLib.Web.Source.Common.Components.ExtDatePickerComponent
{
    public class ExtDatePickerBase : MyComponentBase
    {
        [Parameter]
        public BlazorParameter<string> Placeholder { get; set; }

        [Parameter]
        public BlazorParameter<string> SyncPaddingGroup { get; set; }
        
        [Parameter]
        public BlazorParameter<string> Format { get; set; }
    }
    
    public class ExtDatePickerBase<TProperty> : ExtDatePickerBase, IValidable, IModelable<TProperty>
    {
        private string _displayName;
        private string _propName;
        
        public TelerikDatePicker<TProperty> Tdp { get; set; }

        [Parameter]
        public BlazorParameter<object> Model { get; set; }
        
        [Parameter]
        public BlazorParameter<Expression<Func<TProperty>>> For { get; set; }
        
        [Parameter]
        public BlazorParameter<TProperty> Value { get; set; }

        [Parameter]
        public BlazorParameter<bool?> Validate { get; set; }
        
        [Parameter]
        public BlazorParameter<DateTime?> Min { get; set; }
        
        [Parameter]
        public BlazorParameter<DateTime?> Max { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await Task.CompletedTask;
        }
        
        protected override async Task OnParametersSetAsync()
        {
            if (FirstParamSetup)
            {
                SetMainCustomAndUserDefinedClasses("ext-datepicker", new [] { $"my-guid_{Guid}", $"my-placeholder_{Placeholder.V}", $"my-input-sync-padding-group_{SyncPaddingGroup.V}" });
                SetUserDefinedStyles();
                SetUserDefinedAttributes();
            }
            
            if (Validate.HasChanged())
                Validate.ParameterValue ??= true; 
            
            if (CascadedEditContext.HasChanged())
                Model.ParameterValue ??= CascadedEditContext?.V?.Model;

            if (For.HasChanged() && Model.HasValue())
                (_, _propName, Value, _displayName) = For.V.GetModelAndProperty();

            if (Placeholder.HasChanged())
                Placeholder.ParameterValue = !Placeholder.V.IsNullOrWhiteSpace() ? Placeholder.V : !_displayName.IsNullOrWhiteSpace() ? $"{_displayName}..." : null;
            
            if (Min.HasChanged())
                Min.ParameterValue ??= new DateTime(1900, 1, 1);

            if (Max.HasChanged())
                Max.ParameterValue ??= DateTime.UtcNow - TimeSpanUtils.FromApproximateYears(18);

            if (Format.HasChanged())
                Format.ParameterValue ??= "dd-MM-yyyy";
            
            CascadedEditContext.BindInputValidationStateChanged<ExtDatePickerBase<TProperty>, TProperty>(this);
            
            await Task.CompletedTask;
        }
        
        protected override async Task OnAfterFirstRenderAsync()
        {
            FixNonNativeInputSyncPaddingGroupAndDontAwait();
            await Task.CompletedTask;
        }

        protected async Task DatePicker_ValueChanged(TProperty value)
        {
            if (InteractivityState.V.IsDisabledOrForceDisabled)
                return;

            if (Model.HasValue())
            {
                Model.V.SetProperty(_propName, value);
                Value.ParameterValue = value;
                if (CascadedEditContext?.V is not null)
                    await CascadedEditContext.V.NotifyFieldChangedAsync(new FieldIdentifier(Model.V, _propName), Validate.V == true);
            }
        }
    }
}
