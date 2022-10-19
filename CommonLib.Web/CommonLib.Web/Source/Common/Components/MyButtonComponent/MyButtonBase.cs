using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Models;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils.TypeUtils;
using CommonLib.Web.Source.Common.Components.MyDropDownComponent;
using CommonLib.Web.Source.Common.Components.MyIconComponent;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Truncon.Collections;
using CommonLib.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Converters;

namespace CommonLib.Web.Source.Common.Components.MyButtonComponent
{
    public class MyButtonBase : MyComponentBase
    {
        private readonly SemaphoreSlim _syncValidationStateBeingChanged = new(1, 1);
        private ButtonState? _prevParentState;

        protected BlazorParameter<MyButtonBase> _bpBtn { get; set; }

        public MyIconBase IconBefore { get; set; }
        public MyIconBase IconAfter { get; set; }
        public OrderedDictionary<IconType, MyIconBase> OtherIcons { get; set; }

        public bool IsValidationStateBeingChanged { get; set; }
        
        [CascadingParameter] 
        public BlazorParameter<MyInputBase> CascadingInput { get; set; }
        
        [Parameter]
        public BlazorParameter<object> Model { get; set; }

        [Parameter]
        public BlazorParameter<string> Value { get; set; }

        [Parameter] public BlazorParameter<ButtonState?> State { get; set; }

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
            await Task.CompletedTask;
        }
        
        protected override async Task OnParametersSetAsync()
        {
            if (IsFirstParamSetup())
            {
                SetMainAndUserDefinedClasses("my-btn");
                SetUserDefinedStyles();
                SetUserDefinedAttributes();
            }

            CascadedEditContext.BindValidationStateChanged(CurrentEditContext_ValidationStateChangedAsync);
            
            if (CascadingInput.HasChanged() && CascadingInput.HasValue())
            {
                CascadingInput.ParameterValue.InputGroupButtons.AddIfNotExists(this);
                CascadingInput.SetAsUnchanged(); // so the notify won't end up here again
            }

            var parentStates = Ancestors.Select(a => a.GetPropertyOrNull("State")?.GetPropertyOrNull("ParameterValue").ToComponentStateOrEmpty() ?? ComponentState.Empty).ToArray();
            var parentState = parentStates.All(s => s.State is null) ? (ButtonState?) null : parentStates.Any(s => s.State == ComponentStateKind.Disabled) ? ButtonState.Disabled : parentStates.Any(s => s.State == ComponentStateKind.Loading) ? ButtonState.Loading : ButtonState.Enabled;
            if (State.HasChanged() || CascadingInput.ParameterValue?.State?.HasChanged() == true || parentState != _prevParentState) // || _buttonStateFromValidation != null && State.ParameterValue != _buttonStateFromValidation)
            {
                State.ParameterValue = parentState ?? State.V ?? ButtonState.Disabled; // It has to be overriden at all times by whatever is set to it directly (during the validation)

                if (State.ParameterValue == ButtonState.Loading)
                    AddClasses("my-loading");
                else
                    RemoveClasses("my-loading");

                _prevParentState = parentState;
            }

            if (Styling.HasChanged())
            {
                Styling ??= new BlazorParameter<ButtonStyling?>(ButtonStyling.Secondary);
                Styling.ParameterValue ??= ButtonStyling.Secondary;

                var stylingClass = $"my-btn-{Styling.ParameterValue.EnumToString().ToLowerInvariant()}";
                AddClass(stylingClass);
            }

            if (Sizing.HasChanged())
            {
                Sizing.ParameterValue ??= ButtonSizing.Fill;
                var sizingClass = $"my-{Sizing.ParameterValue.EnumToString().PascalCaseToKebabCase()}";
                AddClass(sizingClass);

                if (Value.V.IsNullOrWhiteSpace()) // remove margins if there is no value (presumably icon only)
                {
                    AddStyles(new Dictionary<string, string> { 
                        ["margin-left"] = "0",
                        ["margin-right"] = "0"
                    });
                }
            }
            
            if (IconPlacement.HasChanged())
                IconPlacement.ParameterValue ??= ButtonIconPlacement.Left;
            if (SubmitsForm.HasChanged())
                SubmitsForm.ParameterValue ??= false;

            var icons = OtherIcons.Values.Prepend(IconBefore).Prepend(IconAfter).Where(i => i is not null).ToArray();
            var changeStateTasks = new List<Task>();
            foreach (var icon in icons)
            {
                if (!icon.CascadingButton.HasValue())
                    icon.CascadingButton.ParameterValue = this; // to solve issue when the parameter is not yet initialized but it needs to be disabled already, for instance before render
                changeStateTasks.Add(icon.NotifyParametersChangedAsync());
                changeStateTasks.Add(icon.StateHasChangedAsync(true));
            }
            await Task.WhenAll(changeStateTasks);
        }

        protected override async Task OnAfterFirstRenderAsync()
        {
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
                State.ParameterValue = ButtonState.Enabled;
            else
            {
                State.ParameterValue = e.ValidationStatus switch
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
        Quadratic,
        LineHeight,
        LineHeightQuadratic
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
