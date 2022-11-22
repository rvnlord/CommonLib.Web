using System.Linq;
using System.Threading.Tasks;
using CommonLib.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Converters;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CommonLib.Web.Source.Common.Components.MyModalComponent
{
    public class MyModalBase : MyComponentBase
    {
        protected ComponentState _prevParentState { get; set; }
        protected MyButtonBase _btnCloseModal { get; set; }

        public ElementReference JsModal { get; set; }

        [Parameter]
        public BlazorParameter<ComponentState> State { get; set; }

        protected override async Task OnInitializedAsync() => await Task.CompletedTask;

        protected override async Task OnParametersSetAsync()
        {
            if (IsFirstParamSetup())
            {
                SetMainAndUserDefinedClasses("my-modal");
                SetUserDefinedStyles();
                SetUserDefinedAttributes();
            }

            var parentStates = Ancestors.Select(a => a.GetPropertyOrNull("State")?.GetPropertyOrNull("ParameterValue").ToComponentStateOrEmpty() ?? ComponentState.Empty).ToArray();
            var parentState = parentStates.All(s => s.State is null) ? null : parentStates.Any(s => s.State.In(ComponentStateKind.Disabled, ComponentStateKind.Loading)) ? ComponentState.Disabled : ComponentState.Enabled;
            if (State.HasChanged() || parentState != _prevParentState)
            {
                State.ParameterValue = parentState.NullifyIf(s => s == _prevParentState) ?? State.V.NullifyIf(s => !State.HasChanged()) ?? ComponentState.Disabled;

                if (State.V.IsDisabledOrForceDisabled)
                {
                    AddAttribute("disabled", string.Empty);
                    AddClass("disabled");
                }
                else
                {
                    RemoveAttribute("disabled");
                    RemoveClass("disabled");
                }

                _prevParentState = parentState;
            }
            
            await Task.CompletedTask;
        }
        
        protected override async Task OnAfterFirstRenderAsync() 
        {
            await (await ModuleAsync).InvokeVoidAsync("blazor_Modal_AfterFirstRender", JsModal).ConfigureAwait(false);
        }

        public async Task ShowModalAsync(bool animate = true) 
        {
            await (await ModuleAsync).InvokeVoidAsync("blazor_Modal_ShowAsync", JsModal, animate).ConfigureAwait(false);
        }

        public async Task HideModalAsync(bool animate = true) 
        {
            await (await ModuleAsync).InvokeVoidAsync("blazor_Modal_HideAsync", JsModal, animate).ConfigureAwait(false);
        }
    }
}
