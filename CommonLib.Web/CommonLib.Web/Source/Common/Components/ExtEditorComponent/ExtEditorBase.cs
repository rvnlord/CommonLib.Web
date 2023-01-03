using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components.Interfaces;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Telerik.Blazor.Components;

namespace CommonLib.Web.Source.Common.Components.ExtEditorComponent
{
    public class ExtEditorBase : MyComponentBase
    {
        public TelerikEditor Te { get; set; }

        [Parameter]
        public BlazorParameter<string> Placeholder { get; set; }

        [Parameter]
        public BlazorParameter<List<IEditorTool>> Tools { get; set; }

        [Parameter]
        public RenderFragment ExtEditorCustomTools { get; set; }
    }
    
    public class ExtEditorBase<TProperty> : ExtEditorBase, IValidable, IModelable<TProperty>
    {
        private string _displayName;
        private string _propName;
        
        [Parameter]
        public BlazorParameter<object> Model { get; set; }
        
        [Parameter]
        public BlazorParameter<Expression<Func<TProperty>>> For { get; set; }
        
        [Parameter]
        public BlazorParameter<TProperty> Value { get; set; }

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
                SetMainAndUserDefinedClasses("ext-editor");
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

            CascadedEditContext.BindInputValidationStateChanged<ExtEditorBase<TProperty>, TProperty>(this);
            
            await Task.CompletedTask;
        }
        
        protected override async Task OnAfterFirstRenderAsync()
        {
            await Task.CompletedTask;
        }

        protected async Task Editor_ValueChanged(string value)
        {
            if (InteractionState.V.IsDisabledOrForceDisabled)
                return;

            if (Model.HasValue())
            {
                Model.V.SetProperty(_propName, value);
                Value.ParameterValue = (TProperty)(object) value;
                if (CascadedEditContext?.V is not null)
                    await CascadedEditContext.V.NotifyFieldChangedAsync(new FieldIdentifier(Model.V, _propName), Validate.V == true);
            }
        }
    }
}
