using System;
using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils.UtilClasses;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace CommonLib.Web.Source.Common.Components.MyCheckBoxComponent
{
    public class MyCheckBoxBase : MyInputBase<bool>
    {
        [Parameter]
        public string Description { get; set; }

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

            Description ??= _propName.AddSpacesToPascalCase();

            Placeholder = !Placeholder.IsNullOrWhiteSpace()
                ? Placeholder
                : !displayName.IsNullOrWhiteSpace()
                    ? $"{displayName}..."
                    : null;
            
            if (State.HasChanged())
            {
                State.ParameterValue ??= InputState.Disabled;
                if (State.ParameterValue.IsDisabled)
                    AddAttribute("disabled", string.Empty);
                else
                    RemoveAttribute("disabled");
            }

            CascadedEditContext.BindValidationStateChanged(CurrentEditContext_ValidationStateChangedAsync);

            await Task.CompletedTask;
        }
        
        protected virtual void CheckBox_Checked(ChangeEventArgs e)
        {
            if (e == null)
                throw new NullReferenceException(nameof(e));

            if (Model != null)
            {
                Model.SetProperty(_propName, e.Value);
                CascadedEditContext.ParameterValue?.NotifyFieldChanged(new FieldIdentifier(Model, _propName));
            }

            Value = e.Value.ToBool();
            Text = Value.ToStringInvariant();
        }
    }
}
