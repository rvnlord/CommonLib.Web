using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils.TypeUtils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Converters;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

namespace CommonLib.Web.Source.Common.Components.MyRadioButtonComponent
{
    public class MyRadioButtonBase : MyComponentBase
    {
        private BlazorParameter<InputState> _bpState;
      
        protected string _propName { get; set; }
        protected string _renderRadioGroup { get; set; }
        protected InputState _prevParentState { get; set; }

        [CascadingParameter(Name = "Model")] 
        public BlazorParameter<object> Model { get; set; }
        
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

        [Parameter]
        public BlazorParameter<string> Description { get; set; }

        [Parameter]
        public BlazorParameter<string> RadioGroup { get; set; }
    }
    
    public class MyRadioButtonBase<TProperty> : MyRadioButtonBase
    {
        public TProperty CurrentPropertyValue => For.GetPropertyValue();
        public bool BoolValue => CurrentPropertyValue.Equals(ValueIfTrue.V);
        public TProperty Value => BoolValue ? ValueIfTrue.V : default;

        [Parameter]
        public Expression<Func<TProperty>> For { get; set; }

        [Parameter]
        public BlazorParameter<TProperty> ValueIfTrue { get; set; }
        
        protected override async Task OnParametersSetAsync()
        {
            if (FirstParamSetup)
            {
                SetMainAndUserDefinedClasses("my-radiobutton");
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

            var (_, propName, _, _) = For.GetModelAndProperty();
            _propName = propName;

            if (Description.HasChanged())
            {
                Description.ParameterValue ??= EnumUtils.IsEnum<TProperty>()
                    ? ValueIfTrue.V.EnumToString()
                    : _propName;
            }

            var parentStates = Ancestors.Select(a => a.GetPropertyOrNull("State")?.GetPropertyOrNull("ParameterValue").ToComponentStateOrEmpty() ?? ComponentState.Empty).ToArray();
            var parentState = parentStates.All(s => s.State is null) ? null : parentStates.Any(s => s.State.In(ComponentStateKind.Disabled, ComponentStateKind.Loading)) ? InputState.Disabled : InputState.Enabled;
            if (State.HasChanged() || parentState != _prevParentState)
            {
                State.ParameterValue = parentState ?? State.V ?? InputState.Disabled;

                if (State.V.In(InputState.Disabled, InputState.ForceDisabled))
                {
                    AddAttribute("disabled", string.Empty);
                    AddClass("disabled");
                }
                else
                {
                    RemoveAttribute("disabled");
                    RemoveClass("disabled");
                }

                _prevParentState = parentState;
            }

            if (RadioGroup.HasChanged())
                _renderRadioGroup = RadioGroup.V.PascalCaseToKebabCase();
            
            CascadedEditContext.BindValidationStateChanged(CurrentEditContext_ValidationStateChangedAsync);
            await Task.CompletedTask;
        }
        
        protected async Task RadioButton_ClickAsync(MouseEventArgs e)
        {
            Model.V.SetPropertyValue(_propName, !BoolValue ? ValueIfTrue.V : ValueIfTrue.V.Equals(default(TProperty)) && EnumUtils.IsEnum<TProperty>() ? EnumUtils.GetValues<TProperty>().Second() : default);
            var radioButtons = await GetRadioButtonsFromThisGroupAsync();
            var refreshTasks = radioButtons.Select(rb => rb.NotifyParametersChangedAsync().StateHasChangedAsync(true)).ToList();
            await Task.WhenAll(refreshTasks);
            CascadedEditContext.ParameterValue?.NotifyFieldChanged(new FieldIdentifier(Model.V, _propName));
        }

        private async Task<MyRadioButtonBase[]> GetRadioButtonsFromThisGroupAsync() => (await ComponentsByTypeAsync<MyRadioButtonBase>()).Where(rb => rb.RadioGroup.V.EqualsIgnoreCase(RadioGroup.V)).ToArray();
        
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