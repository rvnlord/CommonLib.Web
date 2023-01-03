using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components;
using CommonLib.Web.Source.Common.Components.Interfaces;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CommonLib.Web.Source.Common.Extensions
{
    public static class BlazorParameterExtensions
    {
        //public static bool HasChangedOrFalse<T>(this BlazorParameter<T> blazorParam) => blazorParam != null && blazorParam.HasChanged();
        //public static bool HasValueOrFalse<T>(this BlazorParameter<T> blazorParam) => blazorParam != null && blazorParam.HasValue();

        //public static bool HasChangedOrDefault<T>(this BlazorParameter<T> blazorParam)
        //{
        //    blazorParam ??= new BlazorParameter<T>(default);
        //    return blazorParam.HasChanged();
        //}

        //public static bool HasValueOrDefault<T>(this BlazorParameter<T> blazorParam)
        //{
        //    blazorParam ??= new BlazorParameter<T>(default);
        //    return blazorParam.HasValue();
        //}

        //public static bool HasPreviousValueOrDefault<T>(this BlazorParameter<T> blazorParam)
        //{
        //    blazorParam ??= new BlazorParameter<T>(default);
        //    return blazorParam.HasPreviousValue();
        //}

        public static void BindValidationStateChanged(this BlazorParameter<MyEditContext> editContextParam, MyAsyncEventHandler<MyEditContext, MyValidationStateChangedEventArgs> handleValidationStateChanged)
        {
            if (editContextParam.HasChanged())
            {
                if (editContextParam.HasPreviousValue())
                    editContextParam.PreviousParameterValue.OnValidationStateChangedAsync -= handleValidationStateChanged;
                if (editContextParam.HasValue())
                    editContextParam.ParameterValue.ReBindValidationStateChanged(handleValidationStateChanged);
            }
        }

        public static void BindInputValidationStateChanged<TComponent, TProperty>(this BlazorParameter<MyEditContext> editContextParam, TComponent component) where TComponent : MyComponentBase, IModelable<TProperty>, IValidable => editContextParam.BindValidationStateChanged((s, e, t) => CurrentEditContext_InputValidationStateChangedAsync<TComponent, TProperty>(s, e, t, component));

        private static async Task CurrentEditContext_InputValidationStateChangedAsync<TComponent, TProperty>(MyEditContext sender, MyValidationStateChangedEventArgs e, CancellationToken _, TComponent component) where TComponent : MyComponentBase, IModelable<TProperty>, IValidable
        {
            if (e == null)
                throw new NullReferenceException(nameof(e));
            if (e.ValidationMode == ValidationMode.Property && e.ValidatedFields == null)
                throw new NullReferenceException(nameof(e.ValidatedFields));
            if (component.Ancestors.Any(a => a is MyInputBase))
                return;
            if (component.Validate.V != true)
                return;
            if (component.InteractionState.ParameterValue?.IsForced == true)
                return;

            var propName = component.For.V?.GetPropertyName();
            var fi = component.Model.V is not null && propName is not null && !propName.IsNullOrWhiteSpace() ? new FieldIdentifier(component.Model.V, propName) : (FieldIdentifier?)null;
            if (e.ValidationMode == ValidationMode.Model || fi?.In(e.NotValidatedFields) == true || fi?.In(e.ValidatedFields) == true)
                component.RemoveClasses("my-valid", "my-invalid");

            if (e.ValidationMode == ValidationMode.Property && fi is not null && !((FieldIdentifier)fi).In(e.ValidatedFields)) // do nothing if identifier is not propName (if validation is triggered for another field, go ahead if it is propName or if it is null which means we are validating model so there is only one validation changed for all props)
            {
                await component.NotifyParametersChangedAsync().StateHasChangedAsync(true);
                return;
            }
            
            if (sender is null || e.ValidationMode == ValidationMode.Model && e.ValidationStatus.In(ValidationStatus.Pending, ValidationStatus.Success))
            {
                await component.SetControlStateAsync(ComponentState.Disabled, component);
                return;
            }

            if (e.ValidationMode == ValidationMode.Model && e.ValidationStatus == ValidationStatus.Failure)
                await component.SetControlStateAsync(ComponentState.Enabled, component);
            
            if (fi is null)
                return;

            var wasCurrentFieldValidated = propName.In(e.ValidatedFields.Select(f => f.FieldName));
            var isCurrentFieldValid = !propName.In(e.InvalidFields.Select(f => f.FieldName));
            var wasValidationSuccessful = e.ValidationStatus == ValidationStatus.Success;
            var validationFailed = e.ValidationStatus == ValidationStatus.Failure;

            if ((wasValidationSuccessful || isCurrentFieldValid) && wasCurrentFieldValidated)
                component.AddClass("my-valid");
            else if (validationFailed && wasCurrentFieldValidated)
                component.AddClass("my-invalid");

            await  component.NotifyParametersChangedAsync().StateHasChangedAsync(true);
        }
    }
}
