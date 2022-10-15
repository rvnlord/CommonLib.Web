using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Models;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyIconComponent;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Linq;
using CommonLib.Web.Source.Common.Converters;
using CommonLib.Web.Source.Common.Utils.UtilClasses;

namespace CommonLib.Web.Source.Common.Components.MyTextInputComponent
{
    public class MyTextInputBase : MyInputBase<string>
    {
        private readonly SemaphoreSlim _syncJsTextInputAfterRender = new(1, 1);
        
        protected BlazorParameter<MyInputBase> _bpTextInput;
        protected ElementReference _jsTextInput;
      
        protected override async Task OnInitializedAsync()
        {
            _bpTextInput ??= new BlazorParameter<MyInputBase>(this);
            InputGroupButtons ??= new List<MyButtonBase>();
            InputGroupIcons ??= new List<MyIconBase>();
            await Task.CompletedTask.ConfigureAwait(false);
        }

        protected override async Task OnAfterFirstRenderAsync()
        {
            await (await InputModuleAsync).InvokeVoidAndCatchCancellationAsync("blazor_Input_AfterRender", _jsTextInput);
        }

        protected override async Task OnParametersSetAsync() 
        {
            if (IsFirstParamSetup())
            {
                SetMainAndUserDefinedClasses("my-text-input", true);
                SetUserDefinedStyles();

                var customAttrs = new Dictionary<string, string>();
                if (!SyncPaddingGroup.IsNullOrWhiteSpace())
                    customAttrs["my-input-sync-padding-group"] = SyncPaddingGroup;
                SetCustomAndUserDefinedAttributes(customAttrs);
            }

            Model ??= CascadedEditContext?.ParameterValue?.Model; //CurrentEditContext ??= new EditContext(Model);

            string displayName = null;
            if (For != null && Model != null)
                (_, _propName, Value, displayName) = For.GetModelAndProperty();
           
            Text = Value;

            Placeholder = !Placeholder.IsNullOrWhiteSpace()
                ? Placeholder
                : !displayName.IsNullOrWhiteSpace()
                    ? $"{displayName}..."
                    : null;

            var parentStates = Ancestors.Select(a => a.GetPropertyOrNull("State")?.GetPropertyOrNull("ParameterValue").ToComponentStateOrEmpty() ?? ComponentState.Empty).ToArray();
            var parentState = parentStates.All(s => s.State is null) ? null : parentStates.Any(s => s.State.In(ComponentStateKind.Disabled, ComponentStateKind.Loading)) ? InputState.Disabled : InputState.Enabled;
            if (State.HasChanged() || parentState != _prevParentState)
            {
                State.ParameterValue = parentState ?? State.ParameterValue ?? InputState.Disabled; // It has to be overriden at all times by whatever is set to it directly (during the validation)

                if (State.ParameterValue.IsDisabledOrForceDisabled) // Disabled or ForceDisabled
                    AddAttribute("disabled", string.Empty);
                else
                    RemoveAttribute("disabled");
                _prevParentState = parentState;
            }

            CascadedEditContext.BindValidationStateChanged(CurrentEditContext_ValidationStateChangedAsync);

            var notifyParamsChangedTasks = new List<Task>();
            var changeStateTasks = new List<Task>();
            foreach (var inputGroupButton in InputGroupButtons)
            {
                if (!inputGroupButton.CascadingInput.HasValue())
                    inputGroupButton.CascadingInput.ParameterValue = this; // to solve issue when the parameter is not yet initialized but it needs to be disabled already, for instance before render
                notifyParamsChangedTasks.Add(inputGroupButton.NotifyParametersChangedAsync());
                changeStateTasks.Add(inputGroupButton.StateHasChangedAsync(true));
            }

            foreach (var inputGroupIcon in InputGroupIcons)
            {
                if (!inputGroupIcon.CascadingInput.HasValue())
                    inputGroupIcon.CascadingInput.ParameterValue = this; // to solve issue when the parameter is not yet initialized but it needs to be disabled already, for instance before render
                notifyParamsChangedTasks.Add(inputGroupIcon.NotifyParametersChangedAsync());
                changeStateTasks.Add(inputGroupIcon.StateHasChangedAsync(true));
            }

            await Task.WhenAll(notifyParamsChangedTasks);
            await Task.WhenAll(changeStateTasks);
        }
        
        protected void InputText_Input(ChangeEventArgs e)
        {
            if (e == null)
                throw new NullReferenceException(nameof(e));

            if (Model != null)
            {
                Model.SetProperty(_propName, e.Value);
                CascadedEditContext.ParameterValue?.NotifyFieldChanged(new FieldIdentifier(Model, _propName));
            }

            Value = e.Value?.ToString();
            Text = Value;
        }

        protected override async Task DisposeAsync(bool disposing)
        {
            await base.DisposeAsync(disposing);

            if (!disposing)
                return;

            _syncJsTextInputAfterRender.Dispose();
        }
    }
}
