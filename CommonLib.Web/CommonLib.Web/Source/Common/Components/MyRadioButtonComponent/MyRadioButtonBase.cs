using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
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
        protected string _propName { get; set; }
        protected string _renderRadioGroup { get; set; }

        [CascadingParameter(Name = "Model")] 
        public BlazorParameter<object> Model { get; set; }

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
            if (CascadedEditContext?.V is not null)
                await CascadedEditContext.V.NotifyFieldChangedAsync(new FieldIdentifier(Model.V, _propName), true);
        }

        private async Task<MyRadioButtonBase[]> GetRadioButtonsFromThisGroupAsync() => (await ComponentsByTypeAsync<MyRadioButtonBase>()).Where(rb => rb.RadioGroup.V.EqualsIgnoreCase(RadioGroup.V)).ToArray();
        
        private async Task CurrentEditContext_ValidationStateChangedAsync(MyEditContext sender, MyValidationStateChangedEventArgs e, CancellationToken _)
        {
            var fi = new FieldIdentifier(Model.V, _propName);
           
            if (e == null)
                throw new NullReferenceException(nameof(e));
            if (e.ValidationMode == ValidationMode.Property && e.ValidatedFields == null)
                throw new NullReferenceException(nameof(e.ValidatedFields));
            if (InteractivityState.V?.IsForced == true)
                return;
            if (Ancestors.Any(a => a is MyInputBase))
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
                await SetControlStateAsync(ComponentState.Disabled, this);
                return;
            }
            
            var wasCurrentFieldValidated = _propName.In(e.ValidatedFields.Select(f => f.FieldName));
            var isCurrentFieldValid = !_propName.In(e.InvalidFields.Select(f => f.FieldName));
            var wasValidationSuccessful = e.ValidationStatus == ValidationStatus.Success;
            var validationFailed = e.ValidationStatus == ValidationStatus.Failure;

            if ((wasValidationSuccessful || isCurrentFieldValid) && wasCurrentFieldValidated)
                AddClasses("my-valid");
            else if (validationFailed && wasCurrentFieldValidated)
                AddClasses("my-invalid");

            if (e.ValidationMode == ValidationMode.Model && e.ValidationStatus == ValidationStatus.Failure)
                await SetControlStateAsync(ComponentState.Enabled, this);
            else
                await NotifyParametersChangedAsync().StateHasChangedAsync(true);
        }

    }
}