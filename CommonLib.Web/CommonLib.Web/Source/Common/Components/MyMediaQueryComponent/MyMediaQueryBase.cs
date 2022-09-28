using System;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CommonLib.Web.Source.Common.Components.MyMediaQueryComponent
{
    public class MyMediaQueryBase : MyComponentBase
    {
        [Parameter]
        public BlazorParameter<DeviceSizeKind?> DeviceSize { get; set; }

        [Parameter]
        public BlazorParameter<string> Query { get; set; }

        [Parameter]
        public EventCallback<MyMediaQueryChangedEventArgs> OnChange { get; set; }

        //protected override Task OnFirstParametersSetAsync()
        //{
        //    if (FirstParamSetup)
        //    {
        //        SetUserDefinedAttributes();
        //    }
        //}

        protected override async Task OnAfterFirstRenderAsync()
        {
            var mediaQueryDotNetRef = DotNetObjectReference.Create(this);
            await (await ModuleAsync).InvokeVoidAndCatchCancellationAsync("blazor_MediaQuery_AfterFirstRender", Query.V ?? DeviceSize.V?.ToMediaQuery(), DeviceSize.V?.EnumToString().ToLowerInvariant(), _guid, mediaQueryDotNetRef);
        }

        [JSInvokable]
        public async Task MediaQuery_ChangeAsync(string strDeviceSize)
        {
            await OnChangingAsync(strDeviceSize.ToEnum<DeviceSizeKind>());
        }

        private Task OnChangingAsync(DeviceSizeKind deviceSize) => OnChange.InvokeAsync(new MyMediaQueryChangedEventArgs(deviceSize));
    }

    public class MyMediaQueryChangedEventArgs : EventArgs
    {
        public DeviceSizeKind DeviceSize { get; set; }

        public MyMediaQueryChangedEventArgs(DeviceSizeKind deviceSize)
        {
            DeviceSize = deviceSize;
        }
    }
}
