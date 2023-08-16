using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils.TypeUtils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components.Interfaces;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Nethereum.Siwe.Core.Recap;
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
        private DotNetObjectReference<ExtDropDownBase<TProperty>> _extDdlDotNetRef;

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
                SetMainCustomAndUserDefinedClasses("ext-dropdown", new [] { $"my-guid_{Guid}", $"my-placeholder_{Placeholder.V}", $"my-input-sync-padding-group_{SyncPaddingGroup.V}" });
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
                Placeholder.ParameterValue = !Placeholder.V.IsNullOrWhiteSpace() ? Placeholder.V : !_displayName.IsNullOrWhiteSpace() ? $"(Select {_displayName})" : null;

            if (Data.HasChanged())
            {
                Data.ParameterValue ??= typeof(TProperty).EnsureNonNullable().IsEnum ? EnumUtils.EnumToTypedDdlItems<TProperty>().AsEnumerable() : null;
                //if (Data.V is not null && Data.V.Any())
                //{
                //    var t = 0;
                //}
                //if (Data.V is not null && Data.V.GetType().IsIEnumerableType() && Data.V.GetType().GetGenericTypeDefinition() != typeof(DdlItem<,>))
                //    Data.ParameterValue = Data.V.Select((item, index) => new DdlItem<int, object>(index, item.GetPropertyOrNull<string>(TextField.V ?? "Text") ?? index.ToString(), item));
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
            if (!await MyJsRuntime.IsInitializedAsync())
                return;

            FixNonNativeInputSyncPaddingGroupAndDontAwait();
            // bind scrollbar on popup open because after render popup containing list items will be empty

            _extDdlDotNetRef = DotNetObjectReference.Create(this);
            await (await InputModuleAsync).InvokeVoidAndCatchCancellationAsync("blazor_ExtDropDownList_AfterFirstRender", Guid, _extDdlDotNetRef).ConfigureAwait(false);
        }

        [JSInvokable]
        public async Task DropDown_ValueChangedAsync(string itemText)
        {
            if (InteractivityState.V.IsDisabledOrForceDisabled)
                return;

            Tddl.Close();

            if (Model.HasValue())
            {
                var item = Data.V.SingleOrNull(i => i.GetPropertyValue<string>(TextField.V).EqualsIgnoreCase_(itemText));
                var value = item?.GetPropertyOrNull(ValueField.V);
                Model.V.SetProperty(_propName, value);
                Value.ParameterValue = (TProperty) value;
                if (CascadedEditContext?.V is not null)
                    await CascadedEditContext.V.NotifyFieldChangedAsync(new FieldIdentifier(Model.V, _propName), Validate.V == true);
            }
        }
    }
}
