using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Converters;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace CommonLib.Web.Source.Common.Components.MyModalComponent
{
    public class MyModalBase : MyComponentBase
    {
        protected ComponentState PrevParentComponentState { get; set; }
        protected MyButtonBase _btnCloseModal { get; set; }

        public ElementReference JsModal { get; set; }
        
        [Parameter]
        public MyAsyncEventHandler<MyModalBase, EventArgs> Hide { get; set; }

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
        public Task Modal_HideAsync() => Hide.InvokeAsync(this, EventArgs.Empty);
        
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
