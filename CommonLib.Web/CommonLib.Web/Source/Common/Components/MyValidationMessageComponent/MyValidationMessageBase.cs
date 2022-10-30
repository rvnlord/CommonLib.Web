using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components.MyLabelComponent;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.Threading;
using CommonLib.Web.Source.Common.Components.MyInputComponent;

namespace CommonLib.Web.Source.Common.Components.MyValidationMessageComponent
{
    public class MyValidationMessageBase<TProperty> : MyComponentBase
    {
        protected string _propName { get; set; }
        protected string _alignDescriptionClass { get; set; }
        protected string _alignIconClass { get; set; }
        protected string _validationMessage { get; set; }
        protected IconType _iconType { get; set; }
        protected string _iconColor { get; set; }
        protected string _strImageCss { get; set; }
        protected string _alignContainerClass { get; set; }
        protected ElementReference _jsValidationMessage { get; set; }
        protected string _heightClass { get; set; }
        
        public ValidationMessageStatus Status { get; set; }

        [Parameter] 
        public Expression<Func<TProperty>> For { get; set; }

        [CascadingParameter(Name = "Model")] 
        public object Model { get; set; }

        [Parameter] 
        public HorizontalAlignment Align { get; set; }

        [Parameter] 
        public LabelSizing Sizing { get; set; }

        protected override async Task OnInitializedAsync() => await Task.CompletedTask;
        
        protected override async Task OnParametersSetAsync()
        {
            if (IsFirstParamSetup())
            {
                SetMainAndUserDefinedClasses("my-validation-message");
                SetUserDefinedStyles();
                SetUserDefinedAttributes();
            }

            Model ??= CascadedEditContext.ParameterValue.Model;
            CascadedEditContext.ParameterValue ??= new MyEditContext(Model);

            if (For != null && Model != null)
                _propName = For.GetPropertyName();

            _alignIconClass = Align switch
            {
                HorizontalAlignment.Left => "my-validation-message-icon",
                HorizontalAlignment.Center => "my-validation-message-icon my-validation-message-center-icon",
                HorizontalAlignment.Right => "my-validation-message-icon my-validation-message-right-icon",
                _ => ""
            };

            _alignDescriptionClass = Align switch
            {
                HorizontalAlignment.Left => "my-validation-message-description",
                HorizontalAlignment.Center => "my-validation-message-description my-validation-message-center-description",
                HorizontalAlignment.Right => "my-validation-message-description my-validation-message-right-description",
                _ => ""
            };

            _alignContainerClass = Align switch
            {
                HorizontalAlignment.Left => "my-validation-message-icon-and-description-container ",
                HorizontalAlignment.Center => "my-validation-message-icon-and-description-container my-validation-message-center-icon-and-description-container",
                HorizontalAlignment.Right => "my-validation-message-icon-and-description-container my-validation-message-right-icon-and-description-container",
                _ => ""
            };

            _strImageCss = new Dictionary<string, string>
            {
                ["width"] = StylesConfig.LineHeightRem,
                ["height"] = StylesConfig.LineHeightRem,
                ["margin-right"] = StylesConfig.HalfGutter.Px()
            }.CssDictionaryToString();

            if (Sizing == LabelSizing.LineHeight)
                AddStyle("height", StylesConfig.LineHeight.Px());

            CascadedEditContext.BindValidationStateChanged(CurrentEditContext_ValidationStateChangedAsync);

            await Task.CompletedTask;
        }

        protected override async Task OnAfterRenderAsync(bool isFirstRender)
        {
            //if (_iconType == null && _validationMessage == null)
            if (Status == ValidationMessageStatus.NotValidated)
                await (await ModuleAsync).InvokeVoidAndCatchCancellationAsync("blazor_ValidationMessage_HideCol", _jsValidationMessage);
            else
                await (await ModuleAsync).InvokeVoidAndCatchCancellationAsync("blazor_ValidationMessage_ShowCol", _jsValidationMessage);
        }

        private async Task CurrentEditContext_ValidationStateChangedAsync(MyEditContext sender, MyValidationStateChangedEventArgs e, CancellationToken _)
        {
            var fi = new FieldIdentifier(Model, _propName);
            
            if (e == null)
                throw new NullReferenceException(nameof(e));
            if (e.ValidationMode == ValidationMode.Property && e.ValidatedFields == null)
                throw new NullReferenceException(nameof(e.ValidatedFields));
            if (Ancestors.Any(a => a is MyInputBase))
                return;

            if (fi.In(e.NotValidatedFields) || fi.In(e.ValidatedFields))
            {
                _iconType = null;
                _validationMessage = null;
                Status = ValidationMessageStatus.NotValidated;
            }

            if (!fi.In(e.ValidatedFields) && e.ValidationStatus == ValidationStatus.Pending && !fi.In(e.PendingFields)) // do nothing if identifier is is not propName (if validation is triggered for another field, go ahead if it is propName or if it is null which means we are validating model so there is only one validation changed for all props)
            {
                await NotifyParametersChangedAsync().StateHasChangedAsync(true);
                return;
            }

            if (CascadedEditContext == null || e.ValidationMode == ValidationMode.Model && e.ValidationStatus == ValidationStatus.Pending || fi.In(e.PendingFields))
            {
                Status = ValidationMessageStatus.Validating;
                await NotifyParametersChangedAsync().StateHasChangedAsync(true);
                return;
            }
            
            var wasCurrentFieldValidated = _propName.In(e.ValidatedFields.Select(f => f.FieldName));
            var isCurrentFieldValid = !_propName.In(e.InvalidFields.Select(fi => fi.FieldName));
            var wasValidationSuccessful = e.ValidationStatus == ValidationStatus.Success;
            var validationFailed = e.ValidationStatus == ValidationStatus.Failure;
            
            if ((wasValidationSuccessful || isCurrentFieldValid) && wasCurrentFieldValidated)
            {
                _iconColor = StylesConfig.SuccessColor;
                _iconType = IconType.From(DuotoneIconType.Check);
                Status = ValidationMessageStatus.Success;
            }
            else if (validationFailed && wasCurrentFieldValidated)
            {
                _iconColor = StylesConfig.FailureColor;
                _iconType = IconType.From(SolidIconType.Times);
                _validationMessage = CascadedEditContext.ParameterValue.GetValidationMessages(_propName).FirstOrDefault();
                Status = ValidationMessageStatus.Failure;
            }

            await NotifyParametersChangedAsync().StateHasChangedAsync(true);
        }
    }

    public enum ValidationMessageStatus
    {
        NotValidated,
        Success,
        Failure,
        Validating
    }
}
