using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Converters;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Models;
using Microsoft.AspNetCore.Components;

namespace CommonLib.Web.Source.Common.Components.MyProgressBarComponent
{
    public class MyProgressBarBase : MyComponentBase
    {
        private readonly OrderedSemaphore _syncValidationStateBeingChanged = new (1, 1);
        
        protected string _propName { get; set; }

        public double? Value { get; set; }

        [CascadingParameter(Name = "Model")] 
        public BlazorParameter<object> Model { get; set; }

        [Parameter]
        public BlazorParameter<string> Description { get; set; }
        
        [Parameter]
        public Expression<Func<double>> For { get; set; }

        [Parameter]
        public BlazorParameter<ProgressBarStyling?> Styling { get; set; }
        
        [Parameter]
        public BlazorParameter<ProgressBarSizing?> Sizing { get; set; }
        
        [Parameter]
        public BlazorParameter<bool?> Validate { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            if (FirstParamSetup)
            {
                SetMainAndUserDefinedClasses("my-progressbar");
                SetUserDefinedStyles();
                SetUserDefinedAttributes();
            }
            
            if (Validate.HasChanged())
                Validate.ParameterValue ??= true; 
            
            CascadedEditContext.BindValidationStateChanged(CurrentEditContext_ValidationStateChangedAsync);

            if (Model.HasChanged() || CascadedEditContext.HasChanged())
            {
                Model.ParameterValue ??= CascadedEditContext.ParameterValue.Model;
                CascadedEditContext.ParameterValue ??= new MyEditContext(Model.V);
            }
            
            if (For is null && !Model.HasValue())
                return;

            var (_, propName, _, displayName) = For.GetModelAndProperty();
            _propName = propName;
            Value = Math.Min(For.GetPropertyValue(), 100);

            if (Description.HasChanged())
                Description.ParameterValue ??= Model.V?.GetPropertyDescriptionOrNull(_propName) ?? displayName ?? _propName;

            if (Styling.HasChanged())
            {
                Styling.ParameterValue ??= ProgressBarStyling.Primary;
                AddClass(Styling.V.EnumToString().ToLower());
            }

            if (Sizing.HasChanged())
            {
                Sizing.ParameterValue ??= ProgressBarSizing.Default;
                if (Sizing.V == ProgressBarSizing.LineHeight)
                {
                    AddStyles(new Dictionary<string, string>
                    {
                        ["height"] = StylesConfig.LineHeight.Px(),
                        ["padding"] = "0 9px", // teecxhnically not needed because of "align-items",
                        ["font-size"] = "14px"
                    });

                }
                else
                    RemoveStyles(new [] { "height", "padding", "font-size" });
            }
            
            await Task.CompletedTask;
        }

        private async Task CurrentEditContext_ValidationStateChangedAsync(MyEditContext sender, MyValidationStateChangedEventArgs e, CancellationToken _)
        {
            if (e == null)
                throw new NullReferenceException(nameof(e));
            if (e.ValidationMode != ValidationMode.Model)
                return;
            if (InteractivityState.V?.IsForced == true)
                return;
            if (Ancestors.Any(a => a is MyInputBase))
                return;
            if (Validate.V != true)
                return;

            await _syncValidationStateBeingChanged.WaitAsync();
            IsValidationStateBeingChanged = true;

            ComponentState state;
            if (CascadedEditContext == null)
                state = ComponentState.Enabled;
            else
            {
                state = e.ValidationStatus switch
                {
                    ValidationStatus.Pending => ComponentState.Disabled,
                    ValidationStatus.Failure => ComponentState.Enabled,
                    ValidationStatus.Success => ComponentState.Disabled, 
                    _ => ComponentState.Enabled
                };
            }
            
            await SetControlStateAsync(state, this);

            IsValidationStateBeingChanged = false;
            await _syncValidationStateBeingChanged.ReleaseAsync();
        }

        public bool IsValidationStateBeingChanged { get; set; }
    }

    public enum ProgressBarStyling
    {
        Primary,
        Info,
        Success,
        Warning,
        Error
    }

    public enum ProgressBarSizing
    {
        Default,
        LineHeight
    }
}