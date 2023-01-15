using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.TypeUtils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components.Interfaces;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Models;
using MessagePack;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Telerik.Blazor.Components;

namespace CommonLib.Web.Source.Common.Components.ExtDropDownComponent
{
    public class ExtDropDownBase : MyComponentBase
    {
        [Parameter]
        public BlazorParameter<string> Placeholder { get; set; }

        [Parameter]
        public BlazorParameter<string> SyncPaddingGroup { get; set; }
        
        [Parameter]
        public BlazorParameter<string> Format { get; set; }

        [Parameter]
        public BlazorParameter<bool?> Validate { get; set; }

        [Parameter]
        public BlazorParameter<string> TextField { get; set; }

        [Parameter]
        public BlazorParameter<string> ValueField { get; set; }

        [Parameter] 
        public BlazorParameter<IconType> Icon { get; set; }
    }
    
    public class ExtDropDownBase<TProperty> : ExtDropDownBase, IValidable, IModelable<TProperty>
    {
        private string _displayName;
        private string _propName;

        //protected Type _extDdlType { get; set; }
        //protected object _extDdlVal => typeof(TProperty).EnsureNonNullable().IsEnum ? Value.V.ToInt() : Value.V;
        
        public TelerikDropDownList<object, TProperty> Tddl { get; set; }

        [Parameter]
        public BlazorParameter<object> Model { get; set; }
        
        [Parameter]
        public BlazorParameter<Expression<Func<TProperty>>> For { get; set; }
        
        [Parameter]
        public BlazorParameter<TProperty> Value { get; set; }

        [Parameter]
        public BlazorParameter<IEnumerable<object>> Data { get; set; }
        
        protected override async Task OnInitializedAsync()
        {
            await Task.CompletedTask;
        }
        
        protected override async Task OnParametersSetAsync()
        {
            if (FirstParamSetup)
            {
                SetMainCustomAndUserDefinedClasses("ext-dropdown", new [] { $"my-guid_{_guid}", $"my-placeholder_{Placeholder.V}", $"my-input-sync-padding-group_{SyncPaddingGroup.V}" });
                SetUserDefinedStyles();
                SetUserDefinedAttributes();
                //_extDdlType = typeof(TProperty);
            }
            
            if (Validate.HasChanged())
                Validate.ParameterValue ??= true; 
            
            if (CascadedEditContext.HasChanged())
                Model.ParameterValue ??= CascadedEditContext?.V?.Model;

            if (For.HasChanged() && Model.HasValue())
                (_, _propName, Value, _displayName) = For.V.GetModelAndProperty();

            if (Placeholder.HasChanged())
                Placeholder.ParameterValue = !Placeholder.V.IsNullOrWhiteSpace() ? Placeholder.V : !_displayName.IsNullOrWhiteSpace() ? $"{_displayName}..." : null;

            if (Data.HasChanged())
            {
                Data.ParameterValue ??= typeof(TProperty).EnsureNonNullable().IsEnum ? EnumUtils.EnumToTypedDdlItems<TProperty>().AsEnumerable() : null;
                //_extDdlType = typeof(int);
            }

            if (ValueField.HasChanged())
                ValueField.ParameterValue ??= typeof(TProperty).EnsureNonNullable().IsEnum ? "Value" : null;

            if (TextField.HasChanged())
                TextField.ParameterValue ??= typeof(TProperty).EnsureNonNullable().IsEnum ? "Text" : null;

            CascadedEditContext.BindInputValidationStateChanged<ExtDropDownBase<TProperty>, TProperty>(this);
            
            await Task.CompletedTask;
        }
        
        protected override async Task OnAfterFirstRenderAsync()
        {
            if (!MyJsRuntime.IsInitialized)
                return;

            await FixInputSyncPaddingGroupAsync();
            // bind scrollbar on popup open because after render popup containing list items will be empty
        }

        protected async Task DropDown_ValueChanged(TProperty value)
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
