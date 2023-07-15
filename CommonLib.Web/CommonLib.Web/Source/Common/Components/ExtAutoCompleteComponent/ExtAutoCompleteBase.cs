using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components.Interfaces;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Models;
using CommonLib.Web.Source.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Telerik.Blazor.Components;

namespace CommonLib.Web.Source.Common.Components.ExtAutoCompleteComponent
{
    public class ExtAutoCompleteBase : MyComponentBase
    {
        protected string _valueFieldValue { get; set; }

        [Parameter]
        public BlazorParameter<string> Placeholder { get; set; }

        [Parameter]
        public BlazorParameter<string> SyncPaddingGroup { get; set; }

        [Parameter]
        public BlazorParameter<bool?> Filterable { get; set; }

        [Parameter]
        public BlazorParameter<string> ValueField { get; set; }

        [Parameter]
        public BlazorParameter<AutoCompleteMode?> Mode { get; set; }

        [Parameter]
        public RenderFragment ItemTemplate { get; set; }
    }
    
    public class ExtAutoCompleteBase<TProperty> : ExtAutoCompleteBase, IValidable, IModelable<TProperty>
    {
        private string _displayName;
        private string _propName;
        
        public TelerikAutoComplete<TProperty> Tac { get; set; }

        [Parameter]
        public BlazorParameter<IEnumerable<TProperty>> Data { get; set; }

        [Parameter]
        public BlazorParameter<object> Model { get; set; }
        
        [Parameter]
        public BlazorParameter<Expression<Func<TProperty>>> For { get; set; }
        
        [Parameter]
        public BlazorParameter<TProperty> Value { get; set; }

        [Parameter]
        public BlazorParameter<bool?> Validate { get; set; }
        
        [Inject]
        public IJQueryService JQuery { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await Task.CompletedTask;
        }
        
        protected override async Task OnParametersSetAsync()
        {
            if (FirstParamSetup)
            {
                SetMainCustomAndUserDefinedClasses("ext-autocomplete", new[] { $"my-guid_{Guid}", $"my-placeholder_{Placeholder.V}", $"my-input-sync-padding-group_{SyncPaddingGroup.V}" });
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
            
            if (Filterable.HasChanged())
                Filterable.ParameterValue ??= true;
            
            if (Mode.HasChanged())
                Mode.ParameterValue ??= AutoCompleteMode.Default;

            if (ValueField.HasChanged())
            {
                ValueField.ParameterValue ??= Mode.V == AutoCompleteMode.Asset ? nameof(NameWithImage.Name) : null;
                if (Mode.V == AutoCompleteMode.Asset)
                    AddClass("my-k-autocomplete-asset");
            }

            CascadedEditContext.BindInputValidationStateChanged<ExtAutoCompleteBase<TProperty>, TProperty>(this);
            
            await Task.CompletedTask;
        }
        
        protected override async Task OnAfterFirstRenderAsync()
        {
            FixNonNativeInputSyncPaddingGroupAndDontAwait();
        }

        protected async Task AutoComplete_ValueChanged(string valueFieldValue)
        {
            if (InteractivityState.V.IsDisabledOrForceDisabled)
                return;

            if (Model.HasValue())
            {
                var piProperty = Model.V.GetType().GetProperty(_propName);
                var propValue = piProperty?.GetValue(Model.V) ?? piProperty?.PropertyType.GetConstructor(Array.Empty<Type>())?.Invoke(Array.Empty<object>());;

                if (ValueField.HasValue())
                {
                    var piPropertyValueField = piProperty?.PropertyType.GetProperty(ValueField.V);
                    piPropertyValueField?.SetValue(propValue, valueFieldValue);

                    Model.V.SetProperty(_propName, propValue);
                    Value.ParameterValue = (TProperty) propValue;
                }
                else
                {
                    Model.V.SetProperty(_propName, valueFieldValue);
                    Value.ParameterValue = (TProperty) (object) valueFieldValue;
                }

                _valueFieldValue = valueFieldValue;

                if (Mode.V == AutoCompleteMode.Asset)
                {
                    var matchingAsset = Data.V.SingleOrDefault(a => a.GetPropertyValue<string>(ValueField.V).EqualsIgnoreCase(valueFieldValue));
                    var image = matchingAsset?.GetPropertyValue<FileData>(nameof(NameWithImage.Image));
                    var piPropertyImageField = piProperty?.PropertyType.GetProperty(nameof(NameWithImage.Image));
                    piPropertyImageField?.SetValue(propValue, image);

                    var jqTacAsset = await JQuery.QueryOneAsync(Guid);
                    await jqTacAsset.RemoveAsync(".k-autocomplete-symbol");
                    if (image is not null)
                    {
                        var left = (await jqTacAsset.CssAsync("padding-left")).ToDouble() + 5;
                        if ((await jqTacAsset.FindAsync(".k-input-inner")).Single().Classes.Contains("my-ml--5px"))
                            left -= 5;
                        var symbolContent = $"<div class=\"k-autocomplete-symbol\" style=\"background-image: url('{image.ToBase64ImageString()}'); width: 24px; height: 24px; border-radius: 50%; background-size: contain; background-repeat: no-repeat; background-position: center center; box-sizing: border-box; top: 5px; left: {left}px; z-index: 2; position: absolute\"></div>";
                        await jqTacAsset.PrependAsync(symbolContent);
                    }
                }

                if (CascadedEditContext?.V is not null)
                    await CascadedEditContext.V.NotifyFieldChangedAsync(new FieldIdentifier(Model.V, _propName), Validate.V == true);
            }
            
        }
    }

    public enum AutoCompleteMode
    {
        Default,
        Asset
    }
}
