using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components.Interfaces;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Telerik.Blazor.Components;

namespace CommonLib.Web.Source.Common.Components.ExtNumericInputComponent
{
    public class ExtNumericInputBase : MyComponentBase
    {
        [Parameter]
        public BlazorParameter<string> Placeholder { get; set; }

        [Parameter]
        public BlazorParameter<string> SyncPaddingGroup { get; set; }

        [Parameter]
        public BlazorParameter<List<IEditorTool>> Tools { get; set; }

        [Parameter]
        public BlazorParameter<string> Format { get; set; }

        [Parameter]
        public BlazorParameter<int?> Decimals { get; set; }

        [Parameter]
        public RenderFragment ExtEditorCustomTools { get; set; }
    }
    
    public class ExtNumericInputBase<TProperty> : ExtNumericInputBase, IValidable, IModelable<TProperty>
    {
        private string _displayName;
        private string _propName;
        
        public TelerikNumericTextBox<TProperty> TNum { get; set; }

        [Parameter]
        public BlazorParameter<object> Model { get; set; }
        
        [Parameter]
        public BlazorParameter<Expression<Func<TProperty>>> For { get; set; }
        
        [Parameter]
        public BlazorParameter<TProperty> Value { get; set; }

        [Parameter]
        public BlazorParameter<bool?> Validate { get; set; }
        
        [Parameter]
        public BlazorParameter<TProperty> Min { get; set; }
        
        [Parameter]
        public BlazorParameter<TProperty> Max { get; set; }
        
        [Parameter]
        public BlazorParameter<TProperty> Step { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await Task.CompletedTask;
        }
        
        protected override async Task OnParametersSetAsync()
        {
            if (FirstParamSetup)
            {
                SetMainCustomAndUserDefinedClasses("ext-numericinput", new [] { $"my-guid_{_guid}", $"my-placeholder_{Placeholder.V}", $"my-input-sync-padding-group_{SyncPaddingGroup.V}" });
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

            var isNullable = typeof(TProperty).IsGenericType && typeof(TProperty).GetGenericTypeDefinition() == typeof(Nullable<>);
            var directType = isNullable ? Nullable.GetUnderlyingType(typeof(TProperty)) ?? throw new NullReferenceException() : typeof(TProperty);
            var nullableType = typeof(Nullable<>).MakeGenericType(directType);
            //var type = isNullable ? nullableType : directType;

            if (Min.HasChanged())
            {
                var nnDefMin = directType.GetConstructor(new[] { typeof(int) })?.Invoke(new object[] { 0 });
                var defNin = (TProperty) (isNullable ? nullableType.GetConstructor(new[] { directType })?.Invoke(new[] { nnDefMin }) : nnDefMin);
                Min.ParameterValue ??= defNin;
            }

            if (Max.HasChanged())
            {
                var nnDefMax = directType.GetConstructor(new[] { typeof(int) })?.Invoke(new object[] { int.MaxValue });
                var defMax = (TProperty) (isNullable ? nullableType.GetConstructor(new[] { directType })?.Invoke(new[] { nnDefMax }) : nnDefMax);
                Max.ParameterValue ??= defMax;
            }

            if (Step.HasChanged())
            {
                var nnDefStep = directType.GetConstructor(new[] { typeof(int) })?.Invoke(new object[] { 1 });
                var defStep = (TProperty) (isNullable ? nullableType.GetConstructor(new[] { directType })?.Invoke(new[] { nnDefStep }) : nnDefStep);
                Max.ParameterValue ??= defStep;
            }

            if (Format.HasChanged())
                Format.ParameterValue ??= 0.00.ToString();

            if (Decimals.HasChanged())
                Decimals.ParameterValue ??= 2;

            CascadedEditContext.BindInputValidationStateChanged<ExtNumericInputBase<TProperty>, TProperty>(this);
            
            await Task.CompletedTask;
        }
        
        protected override async Task OnAfterFirstRenderAsync()
        {
            await FixInputSyncPaddingGroupAsync();
        }

        protected async Task NumericInput_ValueChanged(TProperty value)
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
