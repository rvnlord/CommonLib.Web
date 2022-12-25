using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components;
using CommonLib.Web.Source.Common.Components.MyFluentValidatorComponent;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Telerik.Blazor.Components;

namespace CommonLib.Web.Source.Common.Utils.UtilClasses
{
    public sealed class MyEditContext
    {
        private readonly Dictionary<FieldIdentifier, MyFieldState> _fieldStates;

        public event MyEventHandler<MyEditContext, MyFieldChangedEventArgs> OnFieldChanged;
        public event MyEventHandler<MyEditContext, ValidationRequestedEventArgs> OnValidationRequested;
        public event MyEventHandler<MyEditContext, MyValidationStateChangedEventArgs> OnValidationStateChanged;
        public event MyAsyncEventHandler<MyEditContext, MyFieldChangedEventArgs> OnFieldChangedAsync;
        public event MyAsyncEventHandler<MyEditContext, ValidationRequestedEventArgs> OnValidationRequestedAsync;
        public event MyAsyncEventHandler<MyEditContext, MyValidationStateChangedEventArgs> OnValidationStateChangedAsync;
        public FieldIdentifier Field(string fieldName) => new(Model, fieldName);
        public object Model { get; }
        public bool IsValid => !GetValidationMessages().Any();

        public MyEditContext(object model)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            _fieldStates = new();
        }

        public async Task NotifyFieldChangedAsync<TProperty>(Expression<Func<TProperty>> accessor, bool shouldValidate)
        {
            var (m, p, _, _) = accessor.GetModelAndProperty();
            await NotifyFieldChangedAsync(new FieldIdentifier(m, p), shouldValidate);
        }

        public async Task NotifyFieldChangedAsync(FieldIdentifier fieldIdentifier, bool shouldValidate)
        {
            GetFieldState(fieldIdentifier, true).IsModified = true;
            OnFieldChanged?.Invoke(this, new MyFieldChangedEventArgs(fieldIdentifier, shouldValidate));
            await OnFieldChangedAsync.InvokeAsync(this, new MyFieldChangedEventArgs(fieldIdentifier, shouldValidate));
        }

        public async Task NotifyValidationStateChangedAsync(ValidationStatus validationStatus, ValidationMode validationMode, List<FieldIdentifier> invalidFields, List<FieldIdentifier> validFields, List<FieldIdentifier> validatedFields, List<FieldIdentifier> notValidatedFields, List<FieldIdentifier> fieldsWithValidationRules, List<FieldIdentifier> fieldsWithoutValidationRules, List<FieldIdentifier> allModelFields, List<FieldIdentifier> pendingFields)
        {
            OnValidationStateChanged?.Invoke(this, new MyValidationStateChangedEventArgs(validationStatus, validationMode, invalidFields, validFields, validatedFields, notValidatedFields, fieldsWithValidationRules, fieldsWithoutValidationRules, allModelFields, pendingFields));
            await OnValidationStateChangedAsync.InvokeAsync(this, new MyValidationStateChangedEventArgs(validationStatus, validationMode, invalidFields, validFields, validatedFields, notValidatedFields, fieldsWithValidationRules, fieldsWithoutValidationRules, allModelFields, pendingFields));
        }

        public async Task NotifyValidationStateChangedAsync(FieldIdentifier? validatedField, MyFluentValidatorBase validator)
        {
            OnValidationStateChanged?.Invoke(this, new MyValidationStateChangedEventArgs(validatedField, validator));
            await OnValidationStateChangedAsync.InvokeAsync(this, new MyValidationStateChangedEventArgs(validatedField, validator));
        }

        public Task NotifyValidationStateChangedAsync(MyFluentValidatorBase validator) => NotifyValidationStateChangedAsync(null, validator);

        public void MarkAsUnmodified(in FieldIdentifier fieldIdentifier)
        {
            if (_fieldStates.TryGetValue(fieldIdentifier, out var state))
                state.IsModified = false;
        }

        public void MarkAsUnmodified()
        {
            foreach (var state in _fieldStates)
                state.Value.IsModified = false;
        }

        public bool IsModified() => _fieldStates.Any(state => state.Value.IsModified);
        public IEnumerable<string> GetValidationMessages() => _fieldStates.SelectMany(state => state.Value.GetValidationMessages());

        public IEnumerable<string> GetValidationMessages(FieldIdentifier fieldIdentifier)
        {
            if (!_fieldStates.TryGetValue(fieldIdentifier, out var state)) 
                yield break;
            foreach (var message in state.GetValidationMessages())
                yield return message;
        }

        public IEnumerable<string> GetValidationMessages(string fieldName) => GetValidationMessages(new FieldIdentifier(Model, fieldName));
        public IEnumerable<string> GetValidationMessages<TProperty>(Expression<Func<TProperty>> accessor) => GetValidationMessages(FieldIdentifier.Create(accessor));
        public bool IsModified(in FieldIdentifier fieldIdentifier) => _fieldStates.TryGetValue(fieldIdentifier, out var state) && state.IsModified;
        public bool IsModified(Expression<Func<object>> accessor) => IsModified(FieldIdentifier.Create(accessor));

        public async Task<bool> ValidateAsync() 
        {
            OnValidationRequested?.Invoke(this, ValidationRequestedEventArgs.Empty);
            await (OnValidationRequestedAsync?.InvokeAsync(this, ValidationRequestedEventArgs.Empty) ?? Task.CompletedTask);
            return !GetValidationMessages().Any();
        }

        public async Task<bool> ValidateFieldAsync<TProperty>(Expression<Func<TProperty>> propertyAccessor)
        {
            var propertyname = propertyAccessor.GetPropertyName();
            var fi = Field(propertyname);
            OnFieldChanged?.Invoke(this, new MyFieldChangedEventArgs(fi, true));
            await (OnFieldChangedAsync?.InvokeAsync(this, new MyFieldChangedEventArgs(fi, true)) ?? Task.CompletedTask);
            return !GetValidationMessages(fi).Any();
        }

        public MyFieldState GetFieldState(in FieldIdentifier fieldIdentifier, bool ensureExists)
        {
            if (!_fieldStates.TryGetValue(fieldIdentifier, out var state) && ensureExists)
            {
                state = new MyFieldState(fieldIdentifier);
                _fieldStates.Add(fieldIdentifier, state);
            }

            return state;
        }

        public void ReBindValidationStateChanged(MyAsyncEventHandler<MyEditContext, MyValidationStateChangedEventArgs> handleValidationStateChanged)
        {
            OnValidationStateChangedAsync -= handleValidationStateChanged;
            OnValidationStateChangedAsync += handleValidationStateChanged;
        }

        public void BindValidationStateChangedForNonNativeComponent<TProperty>(IComponent component, Expression<Func<TProperty>> accessor, MyComponentBase containingComponent)
        {
            ReBindValidationStateChanged((s, e, t) => CurrentEditContext_NonNativeValidationStateChangedAsync(s, e, t, component, accessor, containingComponent));
        }

        private async Task CurrentEditContext_NonNativeValidationStateChangedAsync<TProperty>(MyEditContext sender, MyValidationStateChangedEventArgs e, CancellationToken _, IComponent component, Expression<Func<TProperty>> accessor, MyComponentBase containingComponent)
        {
            if (e == null)
                throw new NullReferenceException(nameof(e));
            if (e.ValidationMode == ValidationMode.Property && e.ValidatedFields == null)
                throw new NullReferenceException(nameof(e.ValidatedFields));

            var propName = accessor.GetPropertyName();
            var fi = Model is not null && !propName.IsNullOrWhiteSpace() ? new FieldIdentifier(Model, propName) : (FieldIdentifier?)null;
            var classes = component.GetPropertyValue<string>("Class").Split(" ");
            if (e.ValidationMode == ValidationMode.Model || fi?.In(e.NotValidatedFields) == true || fi?.In(e.ValidatedFields) == true)
            {
                classes = classes.Except(new[] { "my-valid", "my-invalid" }).ToArray();
                component.SetPropertyValue("Class", classes.JoinAsString(" "));
            }

            if (e.ValidationMode == ValidationMode.Property && fi is not null && !((FieldIdentifier)fi).In(e.ValidatedFields)) // do nothing if identifier is not propName (if validation is triggered for another field, go ahead if it is propName or if it is null which means we are validating model so there is only one validation changed for all props)
            {
                await component.StateHasChangedAsync();
                return;
            }
            
            if (e.ValidationMode == ValidationMode.Model && e.ValidationStatus.In(ValidationStatus.Pending, ValidationStatus.Success))
            {
                await containingComponent.SetControlStateAsync(ComponentState.Disabled, component);
                return;
            }

            if (e.ValidationMode == ValidationMode.Model && e.ValidationStatus == ValidationStatus.Failure)
                await containingComponent.SetControlStateAsync(ComponentState.Enabled, component);

            if (fi is null)
            {
                await component.StateHasChangedAsync();
                return;
            }

            var wasCurrentFieldValidated = propName.In(e.ValidatedFields.Select(f => f.FieldName));
            var isCurrentFieldValid = !propName.In(e.InvalidFields.Select(f => f.FieldName));
            var wasValidationSuccessful = e.ValidationStatus == ValidationStatus.Success;
            var validationFailed = e.ValidationStatus == ValidationStatus.Failure;

            if ((wasValidationSuccessful || isCurrentFieldValid) && wasCurrentFieldValidated)
                classes = classes.Append("my-valid").ToArray();
            else if (validationFailed && wasCurrentFieldValidated)
                classes = classes.Append("my-invalid").ToArray();

            component.SetPropertyValue("Class", classes.JoinAsString(" "));
            await component.StateHasChangedAsync();
        }
    }

    public class MyFieldChangedEventArgs : EventArgs
    {
        public FieldIdentifier FieldIdentifier { get; }
        public bool ShouldValidate { get; }

        public MyFieldChangedEventArgs(FieldIdentifier fieldIdentifier, bool shouldValidate)
        {
            FieldIdentifier = fieldIdentifier;
            ShouldValidate = shouldValidate;
        }
    }

    public class MyValidationStateChangedEventArgs : EventArgs
    {
        public ValidationStatus ValidationStatus { get; }
        public ValidationMode ValidationMode { get; }
        public List<FieldIdentifier> InvalidFields { get; }
        public List<FieldIdentifier> ValidFields { get; }
        public List<FieldIdentifier> ValidatedFields { get; }
        public List<FieldIdentifier> NotValidatedFields { get; }
        public List<FieldIdentifier> FieldsWithValidationRules { get; }
        public List<FieldIdentifier> FieldsWithoutValidationRules { get; }
        public List<FieldIdentifier> AllModelFields { get; }
        public List<FieldIdentifier> PendingFields { get; }

        public MyValidationStateChangedEventArgs(ValidationStatus validationStatus, ValidationMode validationMode, List<FieldIdentifier> invalidFields, List<FieldIdentifier> validFields, List<FieldIdentifier> validatedFields, List<FieldIdentifier> notValidatedFields, List<FieldIdentifier> fieldsWithValidationRules, List<FieldIdentifier> fieldsWithoutValidationRules, List<FieldIdentifier> allModelFields, List<FieldIdentifier> pendingFields)
        {
            ValidationStatus = validationStatus;
            ValidationMode = validationMode;
            InvalidFields = invalidFields;
            ValidFields = validFields;
            ValidatedFields = validatedFields;
            NotValidatedFields = notValidatedFields;
            FieldsWithValidationRules = fieldsWithValidationRules;
            FieldsWithoutValidationRules = fieldsWithoutValidationRules;
            AllModelFields = allModelFields;
            PendingFields = pendingFields;
        }
        
        public MyValidationStateChangedEventArgs(FieldIdentifier? validatedField, MyFluentValidatorBase validator)
        {
            var model = validator.CascadedEditContext.ParameterValue.Model;
            AllModelFields = validator.CascadedEditContext.ParameterValue.Model.GetPropertyNames().Select(p => new FieldIdentifier(model, p)).ToList();
            InvalidFields = validator.MessageStore.GetInvalidFields();
            FieldsWithValidationRules = validator.GetFieldsWithValidationRules();
            ValidatedFields = FieldsWithValidationRules;
            NotValidatedFields = FieldsWithValidationRules.Except(ValidatedFields).ToList();
            ValidFields = FieldsWithValidationRules.Except(InvalidFields).Except(NotValidatedFields).ToList();
            FieldsWithoutValidationRules = AllModelFields.Except(FieldsWithValidationRules).ToList();
            ValidationStatus = InvalidFields.Any() ? ValidationStatus.Failure : ValidationStatus.Success;
            ValidationMode = validatedField == null ? ValidationMode.Model : ValidationMode.Property;
            PendingFields = new List<FieldIdentifier>();
        }
    }

    public enum ValidationStatus
    {
        Failure,
        Success,
        Pending
    }

    public enum ValidationMode
    {
        Model,
        Property
    }
}
