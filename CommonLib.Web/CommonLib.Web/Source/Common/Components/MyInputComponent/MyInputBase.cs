using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CommonLib.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyEditContextComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace CommonLib.Web.Source.Common.Components.MyInputComponent
{
    public abstract class MyInputBase<TProperty> : MyInputBase
    {
        [Parameter]
        public Expression<Func<TProperty>> For { get; set; }
        
        [Parameter]
        public TProperty Value { get; set; }
    }

    public abstract class MyInputBase : MyComponentBase
    {
        protected string _propName { get; set; }
        protected Task<IJSObjectReference> _inputModuleAsync;
        
        public Task<IJSObjectReference> InputModuleAsync => _inputModuleAsync ??= MyJsRuntime.ImportComponentOrPageModuleAsync("my-input", NavigationManager, HttpClient);

        public List<MyButtonBase> InputGroupButtons { get; set; }

        public string Text { get; protected set; }

        [Parameter]
        public object Model { get; set; }

        [Parameter]
        public string Placeholder { get; set; }

        [Parameter]
        public string SyncPaddingGroup { get; set; }

        [Parameter]
        public BlazorParameter<InputState?> State { get; set; }

        protected async Task CurrentEditContext_ValidationStateChangedAsync(object sender, MyValidationStateChangedEventArgs e)
        {
            var fi = new FieldIdentifier(Model, _propName);

            if (e == null)
                throw new NullReferenceException(nameof(e));
            if (e.ValidationMode == ValidationMode.Property && e.ValidatedFields == null)
                throw new NullReferenceException(nameof(e.ValidatedFields));

            if (fi.In(e.NotValidatedFields) || fi.In(e.ValidatedFields))
            {
                RemoveClasses("my-valid", "my-invalid");
                State.ParameterValue = InputState.Enabled;
            }

            if (!fi.In(e.ValidatedFields)) // do nothing if identifier is is not propName (if validation is triggered for another field, go ahead if it is propName or if it is null which means we are validating model so there is only one validation changed for all props)
            {
                await NotifyParametersChangedAsync().StateHasChangedAsync(true);
                return;
            }
            
            if (CascadedEditContext == null || e.ValidationMode == ValidationMode.Model && e.ValidationStatus.In(ValidationStatus.Pending, ValidationStatus.Success))
            {
                State.ParameterValue = InputState.Disabled;
                await NotifyParametersChangedAsync().StateHasChangedAsync(true);
                return;
            }

            var wasCurrentFieldValidated = _propName.In(e.ValidatedFields.Select(f => f.FieldName));
            var isCurrentFieldValid = !_propName.In(e.InvalidFields.Select(fi => fi.FieldName));
            var wasValidationSuccessful = e.ValidationStatus == ValidationStatus.Success;
            var validationFailed = e.ValidationStatus == ValidationStatus.Failure;

            if ((wasValidationSuccessful || isCurrentFieldValid) && wasCurrentFieldValidated)
                AddClasses("my-valid");
            else if (validationFailed && wasCurrentFieldValidated)
                AddClasses("my-invalid");

            await NotifyParametersChangedAsync().StateHasChangedAsync(true);
        }
    }

    public enum InputState
    {
        Enabled,
        Disabled
    }
}
