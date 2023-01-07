using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components.Interfaces;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Models;
using MessagePack;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Telerik.Blazor.Components;

namespace CommonLib.Web.Source.Common.Components.ExtDateTimePickerComponent
{
    public class ExtDateTimePickerBase : MyComponentBase
    {
        [Parameter]
        public BlazorParameter<string> Placeholder { get; set; }

        [Parameter]
        public BlazorParameter<string> SyncPaddingGroup { get; set; }
        
        [Parameter]
        public BlazorParameter<string> Format { get; set; }
    }
    
    public class ExtDateTimePickerBase<TProperty> : ExtDateTimePickerBase, IValidable, IModelable<TProperty>
    {
        private string _displayName;
        private string _propName;
        
        public TelerikDateTimePicker<TProperty> Tdtp { get; set; }

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
                SetMainCustomAndUserDefinedClasses("ext-datetimepicker", new [] { $"my-guid_{_guid}", $"my-placeholder_{Placeholder.V}", $"my-input-sync-padding-group_{SyncPaddingGroup.V}" });
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
                Min.ParameterValue ??= DateTime.UtcNow;

            if (Max.HasChanged())
                Max.ParameterValue ??= DateTime.UtcNow + TimeSpanUtils.FromApproximateMonths(4);

            if (Format.HasChanged())
                Format.ParameterValue ??= "dd-MM-yyyy HH:mm:ss";
            
            CascadedEditContext.BindInputValidationStateChanged<ExtDateTimePickerBase<TProperty>, TProperty>(this);
            
            await Task.CompletedTask;
        }
        
        protected override async Task OnAfterFirstRenderAsync()
        {
            await FixInputSyncPaddingGroupAsync();
        }

        protected async Task DateTimePicker_ValueChanged(TProperty value)
        {
            if (InteractionState.V.IsDisabledOrForceDisabled)
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
