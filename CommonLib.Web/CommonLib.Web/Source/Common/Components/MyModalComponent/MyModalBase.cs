using System;
using System.Linq;
using System.Threading.Tasks;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MoreLinq.Extensions;

namespace CommonLib.Web.Source.Common.Components.MyModalComponent
{
    public class MyModalBase : MyComponentBase
    {
        protected ComponentState PrevParentComponentState { get; set; }
        protected MyButtonBase _btnCloseModal { get; set; }

        public ElementReference JsModal { get; set; }

        [Parameter]
        public BlazorParameter<MyAsyncEventHandler<MyModalBase, EventArgs>> Hide { get; set; } // blazor will force state changed when default (unwrapped in 'BlazorParameter') delegate gets called | this here in turn would cause LoginModal and NavBar to rerender thus breaking auth state checks placed for instance in LoginBase after Signing-In and out (auth would be updated during login navitem change, so later checking if auth state changed in EnsureAuthStateChanged would fail because auth user would have already been set at that point)

        protected override async Task OnInitializedAsync() => await Task.CompletedTask;

        protected override async Task OnParametersSetAsync()
        {
            if (IsFirstParamSetup())
            {
                SetMainAndUserDefinedClasses("my-modal");
                SetUserDefinedStyles();
                SetUserDefinedAttributes();
            }

            await Task.CompletedTask;
        }
        
        protected override async Task OnAfterFirstRenderAsync() 
        {
            await (await ModuleAsync).InvokeVoidAsync("blazor_Modal_AfterFirstRender", JsModal, DotNetObjectReference.Create(this)).ConfigureAwait(false);
        }

        [JSInvokable]
        public async Task Modal_HideAsync()
        {
            await Hide.V.InvokeAsync(this, EventArgs.Empty);
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
