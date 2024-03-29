﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using FluentValidation;
using FluentValidation.Internal;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using MoreLinq;

namespace CommonLib.Web.Source.Common.Components.MyFluentValidatorComponent
{
    public class MyFluentValidatorBase : MyComponentBase
    {
        private MyEditContext _explicitEditContext;
        private IServiceProvider _serviceProvider;
        private MyEditContext _currentEditContext;
        private readonly OrderedSemaphore _syncValidation = new(1, 1);

        public MyValidationMessageStore MessageStore { get; set; }

        [Inject]
        private IServiceProvider ServiceProvider { get; set; }

        private IServiceScope ServiceScope { get; set; }

        [Parameter]
        public IValidator Validator { set; get; }

        [Parameter]
        public Dictionary<Type, IValidator> ChildValidators { set; get; } = new();
        
        protected override async Task OnInitializedAsync()
        {
            await Task.CompletedTask;
        }

        public async Task<MyFluentValidatorBase> InitAsync(MyEditContext editContext, IServiceProvider serviceProvider)
        {
            _explicitEditContext = editContext;
            _serviceProvider = serviceProvider;
            await OnParametersSetAsync();
            return this;
        }

        protected override async Task OnParametersSetAsync()
        {
            _currentEditContext = _explicitEditContext ?? CascadedEditContext?.ParameterValue;
            if (_currentEditContext != null)
            {
                ServiceScope = (_serviceProvider ?? ServiceProvider).CreateScope();

                if (Validator == null)
                    SetFormValidator();
                
                _currentEditContext.OnFieldChangedAsync -= CurrentEditContext_FieldChangedAsync;
                _currentEditContext.OnFieldChangedAsync += CurrentEditContext_FieldChangedAsync;
                _currentEditContext.OnValidationRequestedAsync -= CurrentEditContext_ValidationRequestedAsync;
                _currentEditContext.OnValidationRequestedAsync += CurrentEditContext_ValidationRequestedAsync;
                MessageStore ??= new MyValidationMessageStore(_currentEditContext);
            }


            await Task.CompletedTask;
        }

        protected override async Task OnAfterFirstRenderAsync() => await Task.CompletedTask;

        private async Task CurrentEditContext_ValidationRequestedAsync(object sender, ValidationRequestedEventArgs e, CancellationToken token) => await ValidateModelAsync().ConfigureAwait(false);
        private async Task CurrentEditContext_FieldChangedAsync(object sender, MyFieldChangedEventArgs e, CancellationToken token)
        {
            if (e.ShouldValidate)
                await ValidateFieldAsync(e.FieldIdentifier).ConfigureAwait(false);
        }

        public async Task<bool> ValidateModelAsync(bool changeValidationState = true)
        {
            Validator.SetProperty("ClassLevelCascadeMode", CascadeMode.Stop);

            var model = _currentEditContext.Model;
            var allModelFields = model.GetPropertyNames().Select(p => new FieldIdentifier(model, p)).ToList();
            var vc = new ValidationContext<object>(model);
            var validationResults = await Validator.ValidateAsync(vc).ConfigureAwait(false);
            var validatorCascadeMode = Validator.GetPropertyValue("ClassLevelCascadeMode").ToString();
            var fieldsWithValidationRules = GetFieldsWithValidationRules();
            
            if (changeValidationState)
                await _currentEditContext.NotifyValidationStateChangedAsync(ValidationStatus.Pending, ValidationMode.Model, MessageStore.GetInvalidFields(), new List<FieldIdentifier>(), new List<FieldIdentifier>(), new List<FieldIdentifier>(), new List<FieldIdentifier>(), new List<FieldIdentifier>(), new List<FieldIdentifier>(), fieldsWithValidationRules);
            
            MessageStore.Clear();

            foreach (var error in validationResults.Errors)
                MessageStore.Add(new FieldIdentifier(_currentEditContext.Model, error.PropertyName), error.ErrorMessage);

            var invalidFields = MessageStore.GetInvalidFields();
            var firstInvalidField = invalidFields.FirstOrDefault();
            var validatedFields = validatorCascadeMode.EqualsInvariant("Stop") && invalidFields.Any() ? fieldsWithValidationRules.TakeUntil(f => f.FieldName.EqualsInvariant(firstInvalidField.FieldName)).ToList() : fieldsWithValidationRules;
            var notValidatedFields = fieldsWithValidationRules.Except(validatedFields).ToList();
            var validFields = fieldsWithValidationRules.Except(invalidFields).Except(notValidatedFields).ToList();
            var fieldsWithoutValidationRules = allModelFields.Except(fieldsWithValidationRules).ToList();

            foreach (var validField in validFields)
                MessageStore.GetOrCreateMessagesListForField(validField); // valid fields will have empty message list in store

            var isValid = MessageStore.HasNoMessages();
            if (changeValidationState)
                await _currentEditContext.NotifyValidationStateChangedAsync(
                    isValid ? ValidationStatus.Success : ValidationStatus.Failure,
                    ValidationMode.Model, invalidFields, validFields, validatedFields, notValidatedFields, fieldsWithValidationRules, fieldsWithoutValidationRules, allModelFields, new List<FieldIdentifier>());

            return isValid;
        }
        
        public async Task<bool> ValidateFieldAsync(FieldIdentifier fieldIdentifier, bool changeValidationState = true)
        {
            await _syncValidation.WaitAsync(); // to prevent UI updating with incorrect messages, especially for other fields when this one changed

            var validator = GetFieldValidator(_currentEditContext, fieldIdentifier);
            if (validator is null) // not supposed to be validated
            {
                await _syncValidation.ReleaseAsync();
                return true;
            }
            validator.SetProperty("ClassLevelCascadeMode", CascadeMode.Continue);
            validator.SetProperty("RuleLevelCascadeMode", CascadeMode.Continue);
            
            var model = _currentEditContext.Model; 
            var allModelFields = fieldIdentifier.Model.GetPropertyNames().Select(p => new FieldIdentifier(fieldIdentifier.Model, p)).ToList();
            var fieldsWithValidationRules = GetFieldsWithValidationRules();
            if (!fieldIdentifier.In(fieldsWithValidationRules))
            {
                await _syncValidation.ReleaseAsync();
                return true;
            }

            var validatedFields = new List<FieldIdentifier>(); // check all not empty fields (to accomodate for 'equal' validator)
            foreach (var fwvr in fieldsWithValidationRules)
            {
                if (fwvr.Equals(fieldIdentifier))
                {
                    validatedFields.Add(fwvr); // always validate main property, even if it is empty
                    continue;
                }
                var fwvrVal = model.GetType().GetProperty(fwvr.FieldName)?.GetValue(model);
                if (fwvrVal is null)
                    continue;
                if (fwvrVal is string && fwvrVal.ToString().IsNullOrWhiteSpace())
                    continue;
                if (fwvrVal.ToDoubleN() is not null && fwvrVal.ToDouble().Eq(0))
                    continue;
                validatedFields.Add(fwvr);
            }

            if (changeValidationState)
                await _currentEditContext.NotifyValidationStateChangedAsync(ValidationStatus.Pending, ValidationMode.Property, MessageStore.GetInvalidFields(), new List<FieldIdentifier>(), new List<FieldIdentifier>(), new List<FieldIdentifier>(), new List<FieldIdentifier>(), new List<FieldIdentifier>(), new List<FieldIdentifier>(), new List<FieldIdentifier> { fieldIdentifier });
            
            var vselector = new MemberNameValidatorSelector(validatedFields.Select(f => f.FieldName));
            var vc = new ValidationContext<object>(fieldIdentifier.Model, new PropertyChain(), vselector);
            var vrs = await validator.ValidateAsync(vc).ConfigureAwait(false);
            MessageStore.Clear(validatedFields);

            foreach (var error in vrs.Errors)
                MessageStore.Add(new FieldIdentifier(_currentEditContext.Model, error.PropertyName), error.ErrorMessage);
            
            var invalidFields = MessageStore.GetInvalidFields();
            var notValidatedFields = fieldsWithValidationRules.Except(validatedFields).ToList();
            var validFields = fieldsWithValidationRules.Except(invalidFields).Except(notValidatedFields).ToList();
            var fieldsWithoutValidationRules = allModelFields.Except(fieldsWithValidationRules).ToList();
            
            foreach (var validField in validFields)
                MessageStore.GetOrCreateMessagesListForField(validField); // valid fields will have empty message list in store

            var isValid = MessageStore.HasNoMessages(validatedFields);
            if (changeValidationState)
                await _currentEditContext.NotifyValidationStateChangedAsync(
                    isValid ? ValidationStatus.Success : ValidationStatus.Failure,
                    ValidationMode.Property,
                    invalidFields, validFields, validatedFields, notValidatedFields, fieldsWithValidationRules, fieldsWithoutValidationRules, allModelFields, new List<FieldIdentifier>());

            await _syncValidation.ReleaseAsync();

            return isValid;
        }

        private void SetFormValidator()
        {
            var formType = _currentEditContext.Model.GetType();
            Validator = GetModelValidator(formType);
            if (Validator == null)
                throw new InvalidOperationException($"FluentValidation.IValidator<{formType.FullName}> is not registered in the application service provider.");
        }

        private IValidator GetModelValidator(Type modelType)
        {
            var validatorType = typeof(IValidator<>);
            var formValidatorType = validatorType.MakeGenericType(modelType);
            return ServiceScope.ServiceProvider.GetService(formValidatorType) as IValidator;
        }

        private IValidator GetModelValidator<TModel>() => GetModelValidator(typeof(TModel));

        private IValidator GetFieldValidator(MyEditContext editContext, FieldIdentifier fieldIdentifier)
        {
            if (fieldIdentifier.Model == editContext.Model)
                return Validator;

            var modelType = fieldIdentifier.Model.GetType();
            if (ChildValidators.ContainsKey(modelType))
                return ChildValidators[modelType];
            
            var validator = GetModelValidator(modelType);
            ChildValidators[modelType] = validator;
            return validator;
        }

        public MyEditContext AddValidationMessages(ILookup<string, string> messages)
        {
            if (messages == null)
                return _currentEditContext;

            foreach (var (propertyName, message) in messages.SelectMany(g => g.Select(m => (g.Key, m))))
                MessageStore.Add(new FieldIdentifier(_currentEditContext.Model, propertyName), message);

            return _currentEditContext;
        }

        public List<FieldIdentifier> GetFieldsWithValidationRules()
        {
            var rules = (IEnumerable)Validator.GetPropertyValue("Rules");
            var fieldsWithValidationRules = new List<FieldIdentifier>();
            foreach (var rule in rules)
            {
                var member = rule?.GetPropertyValue("Member");
                var fieldNameWithRule = member?.GetPropertyValue("Name")?.ToString() ?? throw new NullReferenceException("Rule.Member.Name");
                fieldsWithValidationRules.Add(new FieldIdentifier(_currentEditContext.Model, fieldNameWithRule));
            }

            return fieldsWithValidationRules;
        }

        public bool WasValidated<TProperty>(Expression<Func<TProperty>> accessor) => MessageStore.WasValidated(accessor);
        public bool IsValid<TProperty>(Expression<Func<TProperty>> accessor) => MessageStore.IsValid(accessor);

        protected override async Task DisposeAsync(bool disposing)
        {
            if (IsDisposed) 
                return;

            if (disposing)
                ServiceScope?.Dispose();

            ServiceScope = null;
            Validator = null;
            ChildValidators = null;

            await base.DisposeAsync(disposing);

            IsDisposed = true;
        }

        ~MyFluentValidatorBase() 
        {
            _ = DisposeAsync(false);
        }
    }
}
