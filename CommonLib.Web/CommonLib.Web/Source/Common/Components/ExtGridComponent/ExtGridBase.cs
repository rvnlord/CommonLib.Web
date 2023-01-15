using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Source.Common.Extensions;
using CommonLib.Web.Source.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Telerik.Blazor;
using Telerik.Blazor.Components;

namespace CommonLib.Web.Source.Common.Components.ExtGridComponent
{
    public class ExtGridBase : MyComponentBase
    {
        [Parameter]
        public BlazorParameter<GridEditMode> EditMode { get; set; }

        [Parameter]
        public BlazorParameter<bool> Pageable { get; set; }

        [Parameter]
        public BlazorParameter<string> Height { get; set; }

        [Parameter]
        public EventCallback<GridCommandEventArgs> OnCreate { get; set; }
        
        [Parameter]
        public EventCallback<GridCommandEventArgs> OnUpdate { get; set; }
        
        [Parameter]
        public EventCallback<GridCommandEventArgs> OnDelete { get; set; }
        
        [Parameter]
        public EventCallback<GridCommandEventArgs> OnEdit { get; set; }
        
        [Parameter]
        public EventCallback<GridCommandEventArgs> OnAdd { get; set; }
        
        [Parameter]
        public EventCallback<GridCommandEventArgs> OnCancel { get; set; }

        [Parameter]
        public EventCallback<GridReadEventArgs> OnRead { get; set; }

        [Parameter]
        public Action<GridRowRenderEventArgs> OnRowRender { get; set; }

        [Parameter]
        public RenderFragment GridColumns { get; set; }

        [Parameter]
        public RenderFragment GridToolbar { get; set; }

        [Parameter]
        public RenderFragment GridSettings { get; set; }
    }
    
    public class ExtGridBase<TItem> : ExtGridBase
    {
        public TelerikGrid<TItem> Tg { get; set; }

        [Parameter]
        public BlazorParameter<IEnumerable<TItem>> Data { get; set; }
        
        protected override async Task OnInitializedAsync()
        {
            await Task.CompletedTask;
        }
        
        protected override async Task OnParametersSetAsync()
        {
            if (FirstParamSetup)
            {
                SetMainCustomAndUserDefinedClasses("ext-grid", new [] { $"my-guid_{_guid}" });
                SetUserDefinedStyles();
                SetUserDefinedAttributes();
            }
            
            await Task.CompletedTask;
        }
        
        protected override async Task OnAfterFirstRenderAsync()
        {
            if (!MyJsRuntime.IsInitialized)
                return;
            await BindOverlayScrollBar();
        }
    }
}
