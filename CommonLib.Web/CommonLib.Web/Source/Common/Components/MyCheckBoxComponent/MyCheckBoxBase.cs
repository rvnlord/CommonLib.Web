using System;
using System.Linq;
using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Converters;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;

namespace CommonLib.Web.Source.Common.Components.MyCheckBoxComponent
{
    public class MyCheckBoxBase : MyInputBase<bool>
    {
        [Parameter]
        public string Description { get; set; }

        [Parameter]
        public BlazorParameter<bool?> DisplayLabel { get; set; }

        protected override async Task OnInitializedAsync() => await Task.CompletedTask.ConfigureAwait(false);
        protected override async Task OnAfterFirstRenderAsync() => await Task.CompletedTask.ConfigureAwait(false);

        protected override async Task OnParametersSetAsync()
        {
            if (IsFirstParamSetup())
            {
                SetMainAndUserDefinedClasses("my-checkbox");
                SetUserDefinedStyles();
                SetUserDefinedAttributes();
            }

            Model ??= CascadedEditContext?.ParameterValue?.Model;

            string displayName = null;
            if (For != null && Model != null)
                (_, _propName, Value, displayName) = For.GetModelAndProperty();
           
            Text = Value.ToStringInvariant();

            Description ??= Model.GetPropertyDescriptionOrNull(_propName) ?? _propName.AddSpacesToPascalCase();

            Placeholder = !Placeholder.IsNullOrWhiteSpace()
                ? Placeholder
                : !displayName.IsNullOrWhiteSpace()
                    ? $"{displayName}..."
                    : null;
            
            var parentStates = Ancestors.Select(a => a.GetPropertyOrNull("State")?.GetPropertyOrNull("ParameterValue").ToComponentStateOrEmpty() ?? ComponentState.Empty).ToArray();
            var parentState = parentStates.All(s => s.State is null) ? null : parentStates.Any(s => s.State.In(ComponentStateKind.Disabled, ComponentStateKind.Loading)) ? InputState.Disabled : InputState.Enabled;
            if (State.HasChanged() || parentState != _prevParentState)
            {
                State.ParameterValue = parentState.NullifyIf(s => s == _prevParentState) ?? State.V.NullifyIf(s => !State.HasChanged()) ?? InputState.Disabled;

                if (State.ParameterValue.IsDisabledOrForceDisabled)
                    AddAttribute("disabled", string.Empty);
                else
                    RemoveAttribute("disabled");
                _prevParentState = parentState;
            }

            if (DisplayLabel.HasChanged())
                DisplayLabel.ParameterValue ??= true;

            CascadedEditContext.BindValidationStateChanged(CurrentEditContext_ValidationStateChangedAsync); 

            await Task.CompletedTask;
        }
        
        protected virtual void CheckBox_Checked(ChangeEventArgs e)
        {
            if (e == null)
                throw new NullReferenceException(nameof(e));

            if (Model is not null)
            {
                Model.SetProperty(_propName, e.Value);
                CascadedEditContext.ParameterValue?.NotifyFieldChanged(new FieldIdentifier(Model, _propName));
            }

            Value = e.Value.ToBool();
            Text = Value.ToStringInvariant();
        }
    }
}
