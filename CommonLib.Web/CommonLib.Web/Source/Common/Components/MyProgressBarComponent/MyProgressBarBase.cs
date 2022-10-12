using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Models;
using Microsoft.AspNetCore.Components;

namespace CommonLib.Web.Source.Common.Components.MyProgressBarComponent
{
    public class MyProgressBarBase : MyComponentBase
    {
        private BlazorParameter<InputState> _bpState;

        protected string _propName { get; set; }

        public double? Value { get; set; }

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
        public Expression<Func<double>> For { get; set; }

        [Parameter]
        public BlazorParameter<ProgressBarStyling?> Styling { get; set; }
        
        [Parameter]
        public BlazorParameter<ProgressBarSizing?> Sizing { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            if (FirstParamSetup)
            {
                SetMainAndUserDefinedClasses("my-progressbar");
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

            var (_, propName, _, displayName) = For.GetModelAndProperty();
            _propName = propName;
            Value = Math.Min(For.GetPropertyValue(), 100);

            if (Description.HasChanged())
                Description.ParameterValue ??= Model.V?.GetPropertyDescriptionOrNull(_propName) ?? displayName ?? _propName;

            if (State.HasChanged())
            {
                State.ParameterValue ??= InputState.Disabled;

                if (State.V.IsDisabled) 
                {
                    AddAttribute("disabled", string.Empty);
                    AddClass("disabled");
                }
                else
                {
                    RemoveAttribute("disabled");
                    RemoveClass("disabled");
                }
            }

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
                    AddOrUpdateStyles(new Dictionary<string, string>
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