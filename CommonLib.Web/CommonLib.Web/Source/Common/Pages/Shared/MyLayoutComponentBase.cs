using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components;
using CommonLib.Web.Source.Common.Components.MyNavBarComponent;
using CommonLib.Web.Source.Models;
using Microsoft.AspNetCore.Components;

namespace CommonLib.Web.Source.Common.Pages.Shared
{
    public class MyLayoutComponentBase : MyComponentBase
    {
        private OrderedSemaphore _syncComponents = new(1, 1);

        protected ElementReference _jsMyPageScrollContainer { get; set; }
        protected BlazorParameter<MyLayoutComponentBase> _bpLayoutToCascade { get; set; }
        public Dictionary<Guid, MyComponentBase> Components { get; set; }

        public bool AreAllCached
        {
            get
            {
                _syncComponents.Wait();
                var areCached = Components.Values.All(c => c.IsCached);
                _syncComponents.Release();
                return areCached;
            }
        }

        protected override async Task OnInitializedAsync()
        {
            _bpLayoutToCascade = new BlazorParameter<MyLayoutComponentBase>(this);
            Components = new Dictionary<Guid, MyComponentBase>();
            await Task.CompletedTask;
        }
        
        protected override async Task OnAfterFirstRenderWhenAllCachedAsync()
        {
            var navBar = (await GetComponentsSessionCacheAsync()).Components.Values.OfType<MyNavBarBase>().Single();
            await navBar.Setup(_jsMyPageScrollContainer);
        }

        public MyComponentBase AddComponent(MyComponentBase component)
        {
            _syncComponents.Wait();
            Components[component.GetProperty<Guid>("_guid")] = component; 
            _syncComponents.Release();
            return component;
        }

        public event MyAsyncEventHandler<MyComponentBase, LayoutSessionIdSetEventArgs> LayoutSessionIdSet;
        private async Task OnLayoutSessionIdSettingAsync(LayoutSessionIdSetEventArgs e)
        {
            await LayoutSessionIdSet.InvokeAsync(this, e);
            await OnLayoutSessionIdSetAsync(e.Sessionid);
        }
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
