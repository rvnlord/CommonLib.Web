using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Components.MyFluentValidatorComponent;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace CommonLib.Web.Source.Common.Components.MyEditContextComponent
{
    public sealed class MyEditContext
    {
        private readonly Dictionary<FieldIdentifier, MyFieldState> _fieldStates;

        public event EventHandler<FieldChangedEventArgs> OnFieldChanged;
        public event EventHandler<ValidationRequestedEventArgs> OnValidationRequested;
        public event EventHandler<MyValidationStateChangedEventArgs> OnValidationStateChanged;
        public event Func<object, FieldChangedEventArgs, Task> OnFieldChangedAsync;
        public event Func<object, ValidationRequestedEventArgs, Task> OnValidationRequestedAsync;
        public event Func<object, MyValidationStateChangedEventArgs, Task> OnValidationStateChangedAsync;
        public FieldIdentifier Field(string fieldName) => new(Model, fieldName);
        public object Model { get; }
        public bool IsValid => !GetValidationMessages().Any();

        public MyEditContext(object model)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            _fieldStates = new();
        }

        public void NotifyFieldChanged(in FieldIdentifier fieldIdentifier)
        {
            GetFieldState(fieldIdentifier, true).IsModified = true;
            OnFieldChanged?.Invoke(this, new FieldChangedEventArgs(fieldIdentifier));
            OnFieldChangedAsync?.Invoke(this, new FieldChangedEventArgs(fieldIdentifier));
        }

        public void NotifyValidationStateChanged(ValidationStatus validationStatus, ValidationMode validationMode, List<FieldIdentifier> invalidFields, List<FieldIdentifier> validFields, List<FieldIdentifier> validatedFields, List<FieldIdentifier> notValidatedFields, List<FieldIdentifier> fieldsWithValidationRules, List<FieldIdentifier> fieldsWithoutValidationRules, List<FieldIdentifier> allModelFields)
        {
            OnValidationStateChanged?.Invoke(this, new MyValidationStateChangedEventArgs(validationStatus, validationMode, invalidFields, validFields, validatedFields, notValidatedFields, fieldsWithValidationRules, fieldsWithoutValidationRules, allModelFields));
            OnValidationStateChangedAsync?.Invoke(this, new MyValidationStateChangedEventArgs(validationStatus, validationMode, invalidFields, validFields, validatedFields, notValidatedFields, fieldsWithValidationRules, fieldsWithoutValidationRules, allModelFields));
        }

        public void NotifyValidationStateChanged(in FieldIdentifier? validatedField, MyFluentValidatorBase validator)
        {
            OnValidationStateChanged?.Invoke(this, new MyValidationStateChangedEventArgs(validatedField, validator));
            OnValidationStateChangedAsync?.Invoke(this, new MyValidationStateChangedEventArgs(validatedField, validator));
        }

        public void NotifyValidationStateChanged(MyFluentValidatorBase validator) => NotifyValidationStateChanged(null, validator);

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
        //public IEnumerable<string> GetValidationMessages(Expression<Func<object>> accessor) => GetValidationMessages(FieldIdentifier.Create(accessor));
        public IEnumerable<string> GetValidationMessages<TProperty>(Expression<Func<TProperty>> accessor) => GetValidationMessages(FieldIdentifier.Create(accessor));
        public bool IsModified(in FieldIdentifier fieldIdentifier) => _fieldStates.TryGetValue(fieldIdentifier, out var state) && state.IsModified;
        public bool IsModified(Expression<Func<object>> accessor) => IsModified(FieldIdentifier.Create(accessor));

        public async Task<bool> ValidateAsync() 
        {
            OnValidationRequested?.Invoke(this, ValidationRequestedEventArgs.Empty);
            await (OnValidationRequestedAsync?.Invoke(this, ValidationRequestedEventArgs.Empty) ?? Task.CompletedTask);
            return !GetValidationMessages().Any();
        }

        public async Task<bool> ValidateFieldAsync<TProperty>(Expression<Func<TProperty>> propertyAccessor)
        {
            var propertyname = propertyAccessor.GetPropertyName();
            var fi = Field(propertyname);
            OnFieldChanged?.Invoke(this, new FieldChangedEventArgs(fi));
            await (OnFieldChangedAsync?.Invoke(this, new FieldChangedEventArgs(fi)) ?? Task.CompletedTask);
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

        public void ReBindValidationStateChanged(Func<object, MyValidationStateChangedEventArgs, Task> _handleValidationStateChanged)
        {
            OnValidationStateChangedAsync -= _handleValidationStateChanged;
            OnValidationStateChangedAsync += _handleValidationStateChanged;
        }
    }

    public class MyValidationStateChangedEventArgs
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

        public MyValidationStateChangedEventArgs(ValidationStatus validationStatus, ValidationMode validationMode, List<FieldIdentifier> invalidFields, List<FieldIdentifier> validFields, List<FieldIdentifier> validatedFields, List<FieldIdentifier> notValidatedFields, List<FieldIdentifier> fieldsWithValidationRules, List<FieldIdentifier> fieldsWithoutValidationRules, List<FieldIdentifier> allModelFields)
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
