using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace CommonLib.Web.Source.Common.Components.MyPasswordInputComponent
{
    public class MyPasswordInputBase : MyInputBase<string>
    {
        private DotNetObjectReference<MyPasswordInputBase> _passwordInputDotNetRef;

        protected ElementReference _jsPasswordInput;
        protected BlazorParameter<MyInputBase> _bpPasswordInput;
        
        protected override async Task OnInitializedAsync()
        {
            _bpPasswordInput ??= new BlazorParameter<MyInputBase>(this);
            InputGroupButtons ??= new List<MyButtonBase>();
            await Task.CompletedTask;
        }

        protected override async Task OnParametersSetAsync()
        {
            if (IsFirstParamSetup())
            {
                SetMainAndUserDefinedClasses("my-password-input", true);
                SetUserDefinedStyles();
                var customAttrs = new Dictionary<string, string>();
                if (!SyncPaddingGroup.IsNullOrWhiteSpace())
                    customAttrs["my-input-sync-padding-group"] = SyncPaddingGroup;
                SetCustomAndUserDefinedAttributes(customAttrs);
            }

            var editContext = CascadedEditContext?.ParameterValue;
            Model ??= editContext?.Model;

            string displayName = null;
            if (For != null && Model != null)
                (_, _propName, Value, displayName) = For.GetModelAndProperty();
           
            Text = Value;

            Placeholder = !Placeholder.IsNullOrWhiteSpace()
                ? Placeholder
                : !displayName.IsNullOrWhiteSpace()
                    ? $"{displayName}..."
                    : null;

            if (State.ParameterValue == InputState.Disabled)
                AddAttribute("disabled", string.Empty);
            else
                RemoveAttribute("disabled");

            CascadedEditContext?.BindValidationStateChanged(CurrentEditContext_ValidationStateChangedAsync);

            foreach (var inputGroupButton in InputGroupButtons)
            {
                await inputGroupButton.NotifyParametersChangedAsync();
                await inputGroupButton.StateHasChangedAsync(true);
            }
        }

        protected override async Task OnAfterFirstRenderAsync()
        {
            _passwordInputDotNetRef = DotNetObjectReference.Create(this);

            await (await InputModuleAsync).InvokeVoidAsync("blazor_Input_AfterRender", _jsPasswordInput).ConfigureAwait(false);
            await (await ModuleAsync).InvokeVoidAsync("blazor_PasswordInput_AfterRender", Text, _guid, _passwordInputDotNetRef).ConfigureAwait(false);
        }

        [JSInvokable]
        public virtual async Task PasswordInput_InputAsync(string value) // Invoked from javascript
        {
            if (Model != null)
            {
                Model.SetProperty(_propName, value);
                CascadedEditContext.ParameterValue?.NotifyFieldChanged(new FieldIdentifier(Model, _propName));
            }

            Value = value;
            Text = Value;
            
            await StateHasChangedAsync().ConfigureAwait(false); // Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
