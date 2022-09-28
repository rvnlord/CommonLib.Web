using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components;
using CommonLib.Web.Source.Common.Components.MyMediaQueryComponent;
using CommonLib.Web.Source.Common.Components.MyNavBarComponent;
using CommonLib.Web.Source.Common.Components.MyNavLinkComponent;
using CommonLib.Web.Source.Common.Utils;
using CommonLib.Web.Source.Models;
using CommonLib.Web.Source.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CommonLib.Web.Source.Common.Pages.Shared
{
    public class MyLayoutComponentBase : MyComponentBase
    {
        protected ElementReference _jsMyPageScrollContainer { get; set; }
        protected BlazorParameter<MyLayoutComponentBase> _bpLayoutToCascade { get; set; }
        public Dictionary<Guid, MyComponentBase> Components { get; set; }

        internal const string BodyPropertyName = nameof(Body);
        
        [Parameter]
        public RenderFragment Body { get; set; }
        
        protected override async Task OnInitializedAsync()
        {
            _bpLayoutToCascade = new BlazorParameter<MyLayoutComponentBase>(this);
            Components ??= new Dictionary<Guid, MyComponentBase>();
            await Task.CompletedTask;
        }
        
        protected override async Task OnAfterFirstRenderAsync()
        {
            var navBar = await ComponentByTypeAsync<MyNavBarBase>();
            await navBar.Setup(_jsMyPageScrollContainer);
        }
        
        [JSInvokable]
        public static async Task UseNavLinkByGuidAsync(Guid sessionId, Guid navLinkGuid) => (await WebUtils.GetService<ISessionCacheService>()[sessionId].CurrentLayout.ComponentByGuidAsync<MyNavLinkBase>(navLinkGuid)).NavLink_Click();

        public event MyAsyncEventHandler<MyComponentBase, LayoutSessionIdSetEventArgs> LayoutSessionIdSet;
        private async Task OnLayoutSessionIdSettingAsync(LayoutSessionIdSetEventArgs e) => await LayoutSessionIdSet.InvokeAsync(this, e);
        public async Task OnLayoutSessionIdSettingAsync(Guid sessionid) => await OnLayoutSessionIdSettingAsync(new LayoutSessionIdSetEventArgs(sessionid));
        public class LayoutSessionIdSetEventArgs : EventArgs
        {
            public Guid Sessionid { get; }
            
            public LayoutSessionIdSetEventArgs(Guid sessionid)
            {
                Sessionid = sessionid;
            }
        }
    }
}
