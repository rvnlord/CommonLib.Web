using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Components.MyEditContextComponent;
using CommonLib.Web.Source.Common.Converters;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Models;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils.TypeUtils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components.MyIconComponent;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Truncon.Collections;

namespace CommonLib.Web.Source.Common.Components.MyButtonComponent
{
    public class MyButtonBase : MyComponentBase
    {
        private readonly SemaphoreSlim _syncValidationStateBeingChanged = new(1, 1);
        private ButtonState? _buttonStateFromValidation;
        
        protected BlazorParameter<MyButtonBase> _bpBtn;
        protected BlazorParameter<ButtonState?> _bpState;
        protected ButtonState? _state;

        public OrderedDictionary<IconType, MyIconBase> OtherIcons { get; set; }

        public bool IsValidationStateBeingChanged { get; set; }

        [CascadingParameter] 
        public BlazorParameter<InputState> CascadingInputState { get; set; }

        [CascadingParameter] 
        public BlazorParameter<MyInputBase> CascadingInput { get; set; }

        [Parameter]
        public BlazorParameter<object> Model { get; set; }

        [Parameter]
        public BlazorParameter<string> Value { get; set; }

        [Parameter]
        public BlazorParameter<ButtonState?> State { get; set; }

        [Parameter]
        public BlazorParameter<ButtonStyling?> Styling { get; set; }

        [Parameter] 
        public BlazorParameter<ButtonSizing?> Sizing { get; set; }

        [Parameter]
        public BlazorParameter<IconType> Icon { get; set; }

        [Parameter]
        public BlazorParameter<ButtonIconPlacement?> IconPlacement { get; set; }

        [Parameter]
        public BlazorParameter<bool?> SubmitsForm { get; set; }

        [Parameter]
        public EventCallback<MouseEventArgs> OnClick { get; set; }

        protected override async Task OnInitializedAsync()
        {
            OtherIcons ??= new OrderedDictionary<IconType, MyIconBase>();
            _bpBtn ??= new BlazorParameter<MyButtonBase>(this);
            _bpState ??= new BlazorParameter<ButtonState?>(_state);
            await Task.CompletedTask;
            //await OnParametersSetAsync();
        }
        
        protected override async Task OnParametersSetAsync()
        {
            //var eq = Icon.ParameterValue?.LightIcon?.Equals(LightIconType.EyeSlash) ?? false;
            //var eq = OtherIcons.Keys.Any(i => i.Equals(IconType.From(LightIconType.EyeSlash)));
            //if (eq)
            //    eq = eq;

            if (IsFirstParamSetup())
            {
                SetMainAndUserDefinedClasses("my-btn");
                SetUserDefinedStyles();
                SetUserDefinedAttributes();
            }

            CascadedEditContext.BindValidationStateChanged(CurrentEditContext_ValidationStateChangedAsync);

            if (CascadingInput.HasChanged())
                CascadingInput?.ParameterValue?.InputGroupButtons.AddIfNotExists(this);

            if (State.HasChanged() || CascadingInputState.HasChanged() || _buttonStateFromValidation != null && _state != _buttonStateFromValidation)
            {
                ButtonState? cascadingInputState = !CascadingInputState.HasValue() ? null : CascadingInputState.ParameterValue == InputState.Enabled ? ButtonState.Enabled : ButtonState.Disabled;
                _state = _buttonStateFromValidation ?? State.ParameterValue ?? cascadingInputState ?? ButtonState.Enabled; // It has to be overriden at all times by whatever is set to it directly (during the validation)
                _buttonStateFromValidation = null;

                if (_state == ButtonState.Loading)
                    AddClasses("my-loading");
                else
                    RemoveClasses("my-loading");
            }
            
            //Logger.For<MyButtonBase>().Info($"{nameof(OnParametersSetAsync)}(): {Value.ParameterValue}: {_renderClasses}");
            //Logger.For<MyButtonBase>().Info($"{nameof(OnParametersSetAsync)}(), Styling.HasChanged() = {Styling.HasChanged()}");
            if (Styling.HasChanged())
            {
                Styling ??= new BlazorParameter<ButtonStyling?>(ButtonStyling.Secondary);
                Styling.ParameterValue ??= ButtonStyling.Secondary;

                var stylingClass = $"my-btn-{Styling.ParameterValue.EnumToString().ToLowerInvariant()}";
                AddClass(stylingClass);
                //Logger.For<MyButtonBase>().Info($"{nameof(OnParametersSetAsync)}(), Styling.HasChanged(): {Value.ParameterValue}: {_renderClasses}");
            }

            if (Sizing.HasChanged())
            {
                Sizing.ParameterValue ??= ButtonSizing.Fill;
                
                var sizingClass = $"my-{Sizing.ParameterValue.EnumToString().PascalCaseToKebabCase()}";
                AddClass(sizingClass);
            }
            
            if (IconPlacement.HasChanged())
                IconPlacement.ParameterValue ??= ButtonIconPlacement.Left;
            if (SubmitsForm.HasChanged())
                SubmitsForm.ParameterValue ??= false;
            
            await Task.CompletedTask;
        }

        protected override async Task OnAfterFirstRenderAsync()
        {
            // TODO: remove it
            //var eq = OtherIcons.Keys.Any(i => i.Equals(IconType.From(LightIconType.EyeSlash)));
            //if (eq)
            //    eq = eq;

            await Task.CompletedTask;
        }

        protected async void Button_ClickAsync(MouseEventArgs e)
        {
            await OnClick.InvokeAsync(e).ConfigureAwait(false);
        }

        private async Task CurrentEditContext_ValidationStateChangedAsync(object sender, MyValidationStateChangedEventArgs e)
        {
            if (e == null)
                throw new NullReferenceException(nameof(e));
            if (e.ValidationMode != ValidationMode.Model)
                return;

            await _syncValidationStateBeingChanged.WaitAsync();
            IsValidationStateBeingChanged = true;
            
            if (CascadedEditContext == null)
                _buttonStateFromValidation = ButtonState.Enabled;
            else
            {
                _buttonStateFromValidation = e.ValidationStatus switch
                {
                    ValidationStatus.Pending => SubmitsForm.ParameterValue == true ? ButtonState.Loading : ButtonState.Disabled,
                    ValidationStatus.Failure => ButtonState.Enabled,
                    ValidationStatus.Success => ButtonState.Disabled, // disabled regardless because the one thats submtting should not be reenabled between validation and submit so the user has no chance to fuck up the async feature of the whole thing, alt: SubmitsForm.ParameterValue == true ? ButtonState.Disabled : ButtonState.Enabled,
                    _ => ButtonState.Enabled
                };
            }
            
            await NotifyParametersChangedAsync().StateHasChangedAsync(true);

            IsValidationStateBeingChanged = false;
            _syncValidationStateBeingChanged.Release();
        }

        public async Task WaitWhileValidationStateIsBeingChanged() => await TaskUtils.WaitWhile(() => IsValidationStateBeingChanged);
    }

    public enum ButtonStyling
    {
        Primary,
        Secondary,
        Danger,
        Success,
        Info,
        Clear,
        Input
    }

    public enum ButtonSizing
    {
        Fill,
        FillAndDoubleHeight,
        Auto,
        Quadratic
    }

    public enum ButtonState
    {
        Enabled,
        Disabled,
        Loading
    }

    public enum ButtonIconPlacement
    {
        Left,
        Right
    }
}
