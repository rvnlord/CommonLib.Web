using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Utils;
using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Results;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace CommonLib.Web.Source.Common.Extensions
{
    public static class EditContextExtensions
    {
        //public static ValidationMessageStore GetValidationMessages(this EditContext editContext)
        //{
        //    if (editContext == null)
        //        throw new NullReferenceException(nameof(editContext));

        //    var vms = new ValidationMessageStore(editContext);
        //    var model = editContext.Model;
        //    var properties = model.GetType().GetProperties().Select(p => p.Name).ToArray();
            
        //    foreach (var propName in properties)
        //    {
        //        var fieldIdentifier = new FieldIdentifier(model, propName);
        //        var validationMessages = editContext.GetValidationMessages(fieldIdentifier);
        //        vms.Add(fieldIdentifier, validationMessages);
        //    }
        //    return vms;
        //}

        //public static ValidationMessageStore GetValidationMessages<TProperty>(this EditContext editContext, Expression<Func<TProperty>> forProp)
        //{
        //    if (editContext == null)
        //        throw new NullReferenceException(nameof(editContext));
        //    if (forProp == null)
        //        throw new NullReferenceException(nameof(forProp));

        //    var vms = new ValidationMessageStore(editContext);
        //    var propName = forProp.GetPropertyName();
        //    var fieldIdentifier = new FieldIdentifier(editContext.Model, propName);
        //    var validationMessages = editContext.GetValidationMessages(fieldIdentifier);
        //    vms.Add(fieldIdentifier, validationMessages);
        //    return vms;
        //}

        public static ValidationResult GetValidationMessages(this EditContext editContext, string propertyName)
        {
            if (editContext == null)
                throw new NullReferenceException(nameof(editContext));

            var messages = editContext.GetValidationMessages(new FieldIdentifier(editContext.Model, propertyName));
            var vr = new ValidationResult();
            vr.Errors.AddRange(messages.Select(m => new ValidationFailure(propertyName, m)));
            return vr;
        }

        public static ValidationResult ValidateModel(this EditContext editContext)
        {
            var properties = editContext?.Model.GetType().GetProperties().Select(p => p.Name).ToArray() ?? throw new NullReferenceException(nameof(editContext));
            return editContext.ValidateProperties(properties);
        }

        public static ValidationResult ValidateProperty(this EditContext editContext, string property)
        {
            return editContext.ValidateProperties(new [] { property });
        }

        //public static ValidationResult ValidateProperties(this EditContext editContext, IEnumerable<string> properties)
        //{
        //    if (editContext == null)
        //        throw new NullReferenceException(nameof(editContext));

        //    var validatorService = WebUtils.Services.SingleOrDefault(s => 
        //        s.ServiceType.FullName.StartsWithInvariant("FluentValidation.IValidator")
        //            && s.ServiceType.GenericTypeArguments[0].Name.EqualsInvariant(editContext.Model.GetType().Name));

        //    if (validatorService == null)
        //        return new ValidationResult();

        //    var validatorType = validatorService.ImplementationType;
        //    dynamic validator = Activator.CreateInstance(validatorType);
        //    var mValidate = typeof(DefaultValidatorExtensions).GetMethods().Where(m => m.Name.EqualsInvariant("Validate"))
        //        .Where(m =>
        //        {
        //            var parameters = m.GetParameters().ToArray();
        //            return parameters[0].ParameterType.GetGenericTypeDefinition() == typeof(IValidator<>)
        //                   && parameters[1].ParameterType.Name.EqualsInvariant("T")
        //                   && parameters[2].ParameterType == typeof(string[]);
        //        }).FirstOrDefault();

        //    var mgValidate = mValidate?.MakeGenericMethod(editContext.Model.GetType());
        //    return mgValidate?.Invoke(null, new[] { validator, editContext.Model, properties }) as ValidationResult ?? new ValidationResult();
        //}

        public static ValidationResult ValidateProperties(this EditContext editContext, IEnumerable<string> properties)
        {
            if (editContext == null)
                throw new NullReferenceException(nameof(editContext));

            //var serviceScope = WebUtils.ServiceProvider.CreateScope();
            var validatorType = typeof(IValidator<>);
            var formValidatorType = validatorType.MakeGenericType(editContext.Model.GetType());
            var validator = WebUtils.GetService(formValidatorType) as IValidator;

            var vselector = new MemberNameValidatorSelector(properties);
            var vc = new ValidationContext<object>(editContext.Model, new PropertyChain(), vselector);

            var vr = validator?.Validate(vc);
            //serviceScope.Dispose();
            return vr;
        }

        public static async Task<ValidationResult> ValidatePropertyAsync(this EditContext editContext, string property)
        {
            return await Task.Run(() => editContext.ValidateProperty(property)).ConfigureAwait(false);
        }

        public static async Task<ValidationResult> ValidatePropertiesAsync(this EditContext editContext, IEnumerable<string> properties)
        {
            return await Task.Run(() => editContext.ValidateProperties(properties)).ConfigureAwait(false);
        }

        public static async Task<ValidationResult> ValidateModelAsync(this EditContext editContext)
        {
            return await Task.Run(editContext.ValidateModel).ConfigureAwait(false);
        }
    }
}
