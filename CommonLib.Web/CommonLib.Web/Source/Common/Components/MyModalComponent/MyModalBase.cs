﻿using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CommonLib.Web.Source.Common.Components.MyModalComponent
{
    public class MyModalBase : MyComponentBase
    {
        protected MyButtonBase _btnCloseModal { get; set; }

        public ElementReference JsModal { get; set; }

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
            await (await ModuleAsync).InvokeVoidAsync("blazor_Modal_AfterFirstRender", JsModal).ConfigureAwait(false);
        }

        protected override async Task OnAfterRenderAsync(bool _) 
        {
            await (await ModuleAsync).InvokeVoidAsync("blazor_Modal_AfterRender").ConfigureAwait(false);
        }

        public async Task ShowModalAsync() 
        {
            await (await ModuleAsync).InvokeVoidAsync("blazor_Modal_ShowAsync", JsModal).ConfigureAwait(false);
        }

        public async Task HideModalAsync() 
        {
            await (await ModuleAsync).InvokeVoidAsync("blazor_Modal_HideAsync", JsModal).ConfigureAwait(false);
        }
    }
}
