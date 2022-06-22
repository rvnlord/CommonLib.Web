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
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

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
            await Task.CompletedTask.ConfigureAwait(false);
        }

        protected override async Task OnAfterFirstRenderAsync()
        {
            await (await InputModuleAsync).InvokeVoidAsync("blazor_Input_AfterRender", _jsTextInput);
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

            if (State.ParameterValue == InputState.Disabled)
                AddAttribute("disabled", string.Empty);
            else
                RemoveAttribute("disabled");

            CascadedEditContext.BindValidationStateChanged(CurrentEditContext_ValidationStateChangedAsync);

            foreach (var inputGroupButton in InputGroupButtons)
            {
                await inputGroupButton.NotifyParametersChangedAsync();
                await inputGroupButton.StateHasChangedAsync(true);
            }
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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
                return;

            _syncJsTextInputAfterRender.Dispose();
        }
    }
}
