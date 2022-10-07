using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils.TypeUtils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyIconComponent;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Components.MyInputGroupComponent;
using CommonLib.Web.Source.Models;
using Microsoft.JSInterop;
using Truncon.Collections;

namespace CommonLib.Web.Source.Common.Components.MyDropDownComponent
{
    public class MyDropDownBase : MyComponentBase
    {
        private BlazorParameter<InputState> _bpState;
        private Task<IJSObjectReference> _inputModuleAsync;

        protected string _propName { get; set; }
        protected List<DdlItem> _ddlItems { get; set; }
        protected ElementReference _jsDropdown { get; set; }
        protected MyInputGroupBase _inputGroup { get; set; }

        [CascadingParameter(Name = "Model")] 
        public BlazorParameter<object> Model { get; set; }
        
        [Parameter] 
        public BlazorParameter<IconType> Icon { get; set; }

        [Parameter]
        public BlazorParameter<DdlItem> SelectedItem { get; set; }

        [Parameter] 
        public BlazorParameter<DdlItem> EmptyItem { get; set; }

        [Parameter]
        public BlazorParameter<List<DdlItem>> Items { get; set; }

        [Parameter]
        public BlazorParameter<string> SyncPaddingGroup { get; set; }

        [Parameter]
        public BlazorParameter<InputState> State
        {
            get
            {
                return _bpState ??= new BlazorParameter<InputState>(null);
            }
            set
            {

                if (value?.ParameterValue?.IsForced == true && _bpState?.HasValue() == true && _bpState.ParameterValue != value.ParameterValue)
                    throw new Exception("State is forced and it cannot be changed");
                _bpState = value;
            }
        }

        public Task<IJSObjectReference> InputModuleAsync => _inputModuleAsync ??= MyJsRuntime.ImportComponentOrPageModuleAsync(nameof(MyInputBase).BeforeLast("Base"), NavigationManager, HttpClient);
    }
    
    public class MyDropDownBase<TProperty> : MyDropDownBase
    {
        [Parameter] 
        public Expression<Func<TProperty>> For { get; set; }

        [Parameter]
        public BlazorParameter<IEnumerable<TProperty>> PossibleValues { get; set; }

        [Parameter]
        public BlazorParameter<Func<TProperty, DdlItem>> PossibleValuesConverter { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            if (FirstParamSetup)
            {
                SetMainAndUserDefinedClasses("my-dropdown");
                SetUserDefinedStyles();
                SetUserDefinedAttributes();
            }

            if (Model.HasChanged() || CascadedEditContext.HasChanged())
            {
                Model.ParameterValue ??= CascadedEditContext.ParameterValue.Model;
                CascadedEditContext.ParameterValue ??= new MyEditContext(Model.V);
            }
            
            if (For is null && !Model.HasValue())
                return;

            var (_, propName, propValue, displayName) = For.GetModelAndProperty();
            _propName = propName;

            if (Items.HasChanged() || PossibleValues.HasChanged() || PossibleValuesConverter.HasChanged())
            {
                Items.ParameterValue ??= new List<DdlItem>();
                var i = 1;
                _ddlItems = Items.V.Concat(PossibleValues.V?.Select(PossibleValuesConverter.V ?? (p => new DdlItem(i++, p.ToString())))
                    ?? typeof(TProperty).EnumToDdlItems() ?? Enumerable.Empty<DdlItem>()).ToList();
            }

            if (EmptyItem.HasChanged())
                EmptyItem.ParameterValue ??= new DdlItem(null, $"(Select {displayName})");

            if (SelectedItem.HasChanged())
            {
                if (!SelectedItem.HasValue())
                {
                    if (EnumUtils.IsEnum<TProperty>())
                        SelectedItem = _ddlItems.SingleOrDefault(item => item.Index == propValue?.ToIntN());
                    else
                        SelectedItem = _ddlItems.SingleOrDefault(item => item.Text == (PossibleValuesConverter.HasValue() ? PossibleValuesConverter.V(propValue).Text : propValue?.ToString()));
                }
                if (!SelectedItem.HasValue()) // if it still doesn't havee value
                    SelectedItem = EmptyItem;
            }

            if (State.HasChanged())
            {
                State.ParameterValue ??= InputState.Disabled;

                if (State.ParameterValue.IsDisabled) // Disabled or ForceDisabled
                {
                    AddAttribute("disabled", string.Empty);
                    AddClass("disabled");
                }
                else
                {
                    RemoveAttribute("disabled");
                    RemoveClass("disabled");
                }
            }

            if (SyncPaddingGroup.HasChanged() && SyncPaddingGroup.V?.IsNullOrWhiteSpace() != true)
                AddAttribute("my-input-sync-padding-group", SyncPaddingGroup.V);

            CascadedEditContext.BindValidationStateChanged(CurrentEditContext_ValidationStateChangedAsync);

            var notifyParamsChangedTasks = new List<Task>();
            var changeStateTasks = new List<Task>();
            var inputGroupAppendAndPrepend = _inputGroup?.Children; // don't override parent for it, this component is designed in such a way that InputGroup is defined directly within it so InputGroup Parent would not bee MyDropdown but ratheer MyCssGridItem or sth else. Overriding parent directly would create infinite recursion of ancestors (InputGroup < MyDropdown < ...)
            var inputGroupButtons = inputGroupAppendAndPrepend?.SelectMany(c => c.Children.OfType<MyButtonBase>()).ToArray() ?? Array.Empty<MyButtonBase>();
            var inputGroupIcons = inputGroupAppendAndPrepend?.SelectMany(c => c.Children.OfType<MyIconBase>()).ToArray() ?? Array.Empty<MyIconBase>();
            foreach (var inputGroupButton in inputGroupButtons)
            {
                notifyParamsChangedTasks.Add(inputGroupButton.NotifyParametersChangedAsync());
                changeStateTasks.Add(inputGroupButton.StateHasChangedAsync(true));
            }

            foreach (var inputGroupIcon in inputGroupIcons)
            {
                notifyParamsChangedTasks.Add(inputGroupIcon.NotifyParametersChangedAsync());
                changeStateTasks.Add(inputGroupIcon.StateHasChangedAsync(true));
            }

            await Task.WhenAll(notifyParamsChangedTasks);
            await Task.WhenAll(changeStateTasks);
        }
        
        protected override async Task OnAfterFirstRenderAsync()
        {
            await ModuleAsync; // program don't need top call the module but JQyery events need to be available
            await (await InputModuleAsync).InvokeVoidAndCatchCancellationAsync("blazor_Input_AfterRender", _jsDropdown);
        }

        protected async Task DdlOption_ClickAsync(MouseEventArgs e, int? index, Guid ddlGuid)
        {
            if (State.V.IsDisabled)
                return;

            await (await ModuleAsync).InvokeVoidAndCatchCancellationAsync("blazor_DdlOption_ClickAsync", e, index, ddlGuid);
            var selectedItemWithMappedindex = _ddlItems.SingleOrDefault(i => i.Index == index);
            SelectedItem.ParameterValue = selectedItemWithMappedindex ?? EmptyItem.V;
            if (Model != null && For != null)
            {
                var (_, propName, oldValue, _) = For.GetModelAndProperty();
                var possibleVals = (PossibleValues.V ?? EnumUtils.GetValuesOrNull<TProperty>() ?? Enumerable.Empty<TProperty>()).ToArray();

                TProperty newValue;
                if (index == null || index == EmptyItem.V.Index)
                    newValue = default;
                else if (EnumUtils.IsEnum<TProperty>())
                    newValue = possibleVals.FirstOrDefault(en => en.ToInt() == index);
                else
                {
                    var ddlItemWithIndex = _ddlItems.Single(di => di.Index == index);
                    newValue = possibleVals.SingleOrDefault(item => (PossibleValuesConverter.V is not null
                        ? PossibleValuesConverter.V(item).Text
                        : item.ToString()) == ddlItemWithIndex.Text);
                }

                if (!Equals(oldValue, newValue))
                {
                    For.SetPropertyValue(newValue);
                    CascadedEditContext.ParameterValue.NotifyFieldChanged(new FieldIdentifier(Model.V, propName));
                }
            }
        }

        private async Task CurrentEditContext_ValidationStateChangedAsync(object sender, MyValidationStateChangedEventArgs e)
        {
            var fi = new FieldIdentifier(Model.V, _propName);

            if (e == null)
                throw new NullReferenceException(nameof(e));
            if (e.ValidationMode == ValidationMode.Property && e.ValidatedFields == null)
                throw new NullReferenceException(nameof(e.ValidatedFields));
            if (State.ParameterValue?.IsForced == true)
                return;

            if (e.ValidationMode == ValidationMode.Model || fi.In(e.NotValidatedFields) || fi.In(e.ValidatedFields))
                RemoveClasses("my-valid", "my-invalid");

            if (e.ValidationMode == ValidationMode.Property && !fi.In(e.ValidatedFields)) // do nothing if identifier is is not propName (if validation is triggered for another field, go ahead if it is propName or if it is null which means we are validating model so there is only one validation changed for all props)
            {
                await NotifyParametersChangedAsync().StateHasChangedAsync(true);
                return;
            }
            
            if (CascadedEditContext == null || e.ValidationMode == ValidationMode.Model && e.ValidationStatus.In(ValidationStatus.Pending, ValidationStatus.Success))
            {
                State.ParameterValue = InputState.Disabled; // new InputState(InputStateKind.Disabled, State.ParameterValue?.IsForced == true); // not needed because we won't end up here if state is forced
                await NotifyParametersChangedAsync().StateHasChangedAsync(true);
                return;
            }

            if (e.ValidationMode == ValidationMode.Model && e.ValidationStatus == ValidationStatus.Failure)
                State.ParameterValue = InputState.Enabled;

            var wasCurrentFieldValidated = _propName.In(e.ValidatedFields.Select(f => f.FieldName));
            var isCurrentFieldValid = !_propName.In(e.InvalidFields.Select(f => f.FieldName));
            var wasValidationSuccessful = e.ValidationStatus == ValidationStatus.Success;
            var validationFailed = e.ValidationStatus == ValidationStatus.Failure;

            if ((wasValidationSuccessful || isCurrentFieldValid) && wasCurrentFieldValidated)
                AddClasses("my-valid");
            else if (validationFailed && wasCurrentFieldValidated)
                AddClasses("my-invalid");

            await NotifyParametersChangedAsync().StateHasChangedAsync(true);
        }
    }
}