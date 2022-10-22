using System;
using System.Collections.Concurrent;
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
        private Task<IJSObjectReference> _mediaQueryModuleAsync;
        protected internal OrderedSemaphore _syncComponentsCache { get; } = new(1, 1);
        protected internal BlazorParameter<MyLayoutComponentBase> _bpLayoutToCascade { get; set; }
        public ConcurrentDictionary<Guid, MyComponentBase> Components { get; set; }

        internal const string BodyPropertyName = nameof(Body);
        public Task<IJSObjectReference> MediaQueryModuleAsync => _mediaQueryModuleAsync ??= MyJsRuntime.ImportComponentOrPageModuleAsync(nameof(MyMediaQueryBase).BeforeLast("Base"), NavigationManager, HttpClient);

        
        [Parameter]
        public RenderFragment Body { get; set; }

        public DeviceSizeKind? DeviceSize { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Components ??= new ConcurrentDictionary<Guid, MyComponentBase>();
            await Task.CompletedTask;
        }
        
        //[JSInvokable]
        //public static async Task UseNavLinkByGuidAsync(Guid sessionId, Guid navLinkGuid) => (await WebUtils.GetService<ISessionCacheService>()[sessionId].CurrentLayout.ComponentByGuidAsync<MyNavLinkBase>(navLinkGuid)).NavLink_Click(); // used directly to avoid storing data in Singleton service and wasting server memory

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

        [JSInvokable]
        public async Task MediaQuery_ChangeAsync(string strDeviceSize)
        {
            DeviceSize = strDeviceSize.ToEnum<DeviceSizeKind>();
            await OnDeviceSizeChangingAsync((DeviceSizeKind)DeviceSize);
        }
        
        public event MyAsyncEventHandler<MyLayoutComponentBase, MyMediaQueryChangedEventArgs> DeviceSizeChanged;
        private async Task OnDeviceSizeChangingAsync(MyMediaQueryChangedEventArgs e) => await DeviceSizeChanged.InvokeAsync(this, e);
        private async Task OnDeviceSizeChangingAsync(DeviceSizeKind deviceSize) => await OnDeviceSizeChangingAsync(new MyMediaQueryChangedEventArgs(deviceSize));
    }
}
