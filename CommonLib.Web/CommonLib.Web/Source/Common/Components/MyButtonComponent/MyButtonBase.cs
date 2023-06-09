using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Models;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils.TypeUtils;
using CommonLib.Web.Source.Common.Components.MyIconComponent;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Components.MyInputGroupComponent;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Truncon.Collections;

namespace CommonLib.Web.Source.Common.Components.MyButtonComponent
{
    public class MyButtonBase : MyComponentBase
    {
        private readonly SemaphoreSlim _syncValidationStateBeingChanged = new(1, 1);
        
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
        public BlazorParameter<bool?> PreventMultiClicks { get; set; }

        [Parameter]
        public BlazorParameter<bool?> Validate { get; set; }

        [Parameter]
        public EventCallback<MouseEventArgs> OnClick { get; set; } // for backwards compatibility
        
        [Parameter]
        public MyAsyncEventHandler<MyButtonBase, MouseEventArgs> Click { get; set; }

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
            
            //if (AdditionalAttributesHaveChanged) // if class changes but it is not first render then component wouldn't pick up that class by itself (i.e.: in FileUpload 'fileCssClass')
            //    AddAttributes(AdditionalAttributes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString()));
            // not like this, class and style shouldn't be changed in attrs and changing class would override classes set on other Params changee

            if (Validate.HasChanged())
                Validate.ParameterValue ??= true; 
          
            CascadedEditContext.BindValidationStateChanged(CurrentEditContext_ValidationStateChangedAsync);
            
            if (CascadingInput.HasChanged() && CascadingInput.HasValue())
            {
                CascadingInput.ParameterValue.InputGroupButtons.AddIfNotExists(this);
                CascadingInput.SetAsUnchanged(); // so the notify won't end up here again
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

                if (Value.V.IsNullOrWhiteSpace() && !Classes.ContainsAny("my-btn-clear")) // remove margins if there is no value (presumably icon only)
                {
                    AddStyles(new Dictionary<string, string> { 
                        ["margin-left"] = "0",
                        ["margin-right"] = "0"
                    });
                }
            }

            if (PreventMultiClicks.HasChanged())
                PreventMultiClicks.ParameterValue ??= true;
            
            if (IconPlacement.HasChanged())
                IconPlacement.ParameterValue ??= ButtonIconPlacement.Left;
            if (SubmitsForm.HasChanged())
                SubmitsForm.ParameterValue ??= false;

            //var icons = OtherIcons.Values.Prepend(IconBefore).Prepend(IconAfter).Where(i => i is not null).ToArray();
            //var changeStateTasks = new List<Task>();
            //foreach (var icon in icons)
            //{
            //    if (!icon.CascadingButton.HasValue())
            //        icon.CascadingButton.ParameterValue = this; // to solve issue when the parameter is not yet initialized but it needs to be disabled already, for instance before render
            //    changeStateTasks.Add(icon.NotifyParametersChangedAsync());
            //    changeStateTasks.Add(icon.StateHasChangedAsync(true));
            //}
            //await Task.WhenAll(changeStateTasks);

            await Task.CompletedTask;
        }
        
        protected override async Task OnAfterFirstRenderAsync()
        {
            await Task.CompletedTask;
        }

        protected async Task Button_ClickAsync(MouseEventArgs e)
        {
            if (InteractivityState.V.IsDisabledOrForceDisabled || e.Button != 0 || (PreventMultiClicks.V == true && e.Detail > 1))
                return;

            await OnClick.InvokeAsync(e).ConfigureAwait(false);
            await Click.InvokeAsync(this, e);
        }

        private async Task CurrentEditContext_ValidationStateChangedAsync(MyEditContext sender, MyValidationStateChangedEventArgs e, CancellationToken token)
        {
            if (e == null)
                throw new NullReferenceException(nameof(e));
            if (Ancestors.Any(a => a is MyInputBase or MyInputGroupBase))
                return;
            if (Validate.V != true)
                return;
            if (e.ValidationMode != ValidationMode.Model)
                return;

            await _syncValidationStateBeingChanged.WaitAsync(token);
            IsValidationStateBeingChanged = true;
            
            if (CascadedEditContext == null)
                await SetControlStateAsync(ComponentState.Enabled, this);
            else
            {
                var state = e.ValidationStatus switch
                {
                    ValidationStatus.Pending => SubmitsForm.ParameterValue == true ? ComponentState.Loading : ComponentState.Disabled,
                    ValidationStatus.Failure => ComponentState.Enabled,
                    ValidationStatus.Success => SubmitsForm.ParameterValue == true ? ComponentState.Loading : ComponentState.Disabled, // disabled regardless because the one thats submtting should not be reenabled between validation and submit so the user has no chance to fuck up the async feature of the whole thing, alt: SubmitsForm.ParameterValue == true ? ButtonState.Disabled : ButtonState.Enabled,
                    _ => ComponentState.Enabled
                };
                await SetControlStateAsync(state, this);
            }

            IsValidationStateBeingChanged = false;
            _syncValidationStateBeingChanged.Release();
        }

        public async Task WaitWhileValidationStateIsBeingChanged() => await TaskUtils.WaitWhileAsync(() => IsValidationStateBeingChanged);
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

    //public enum ButtonState
    //{
    //    Enabled,
    //    Disabled,
    //    Loading
    //}

    public enum ButtonIconPlacement
    {
        Left,
        Right
    }
}
