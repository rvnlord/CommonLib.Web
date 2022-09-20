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
        protected ElementReference _jsMyPageScrollContainer { get; set; }
        protected BlazorParameter<MyLayoutComponentBase> _bpLayoutToCascade { get; set; }
        public Dictionary<Guid, MyComponentBase> Components { get; set; }
        public Dictionary<Guid, MyComponentBase> CachedComponents { get; set; }

        internal const string BodyPropertyName = nameof(Body);

        public bool WereAllCachedAtleastOnce { get; private set; }
        public bool AreAllCachedForTheFirstTime => !WereAllCachedAtleastOnce && (WereAllCachedAtleastOnce = CachedComponents.Count == Components.Count);

        [Parameter]
        public RenderFragment Body { get; set; }
        
        protected override async Task OnInitializedAsync()
        {
            _bpLayoutToCascade = new BlazorParameter<MyLayoutComponentBase>(this);
            Components ??= new Dictionary<Guid, MyComponentBase>();
            CachedComponents ??= new Dictionary<Guid, MyComponentBase>();
            await Task.CompletedTask;
        }
        
        protected override async Task OnAfterFirstRenderWhenAllCachedAsync()
        {
            var navBar = (await GetComponentsSessionCacheAsync()).Components.Values.OfType<MyNavBarBase>().Single();
            await navBar.Setup(_jsMyPageScrollContainer);
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

        public event MyAsyncEventHandler<MyComponentBase, AfterCurrentComponentFirstRenderedAndAllCachedEventArgs> AfterCurrentComponentFirstRenderedAndAllCached;
        private async Task OnAfterCurrentComponentFirstRenderedAndAllCachingAsync(AfterCurrentComponentFirstRenderedAndAllCachedEventArgs e)
        {
            await AfterCurrentComponentFirstRenderedAndAllCached.InvokeAsync(this, e);
            //Logger.For<MyComponentBase>().Info($"[{GetType().Name}] Called: OnAfterFirstRenderWhenAllCachedAsync()");
        }
        public async Task OnAfterCurrentComponentFirstRenderedAndAllCachingAsync() => await OnAfterCurrentComponentFirstRenderedAndAllCachingAsync(new AfterCurrentComponentFirstRenderedAndAllCachedEventArgs());
        public class AfterCurrentComponentFirstRenderedAndAllCachedEventArgs : EventArgs
        {
            public AfterCurrentComponentFirstRenderedAndAllCachedEventArgs() { }
        }
    }
}
