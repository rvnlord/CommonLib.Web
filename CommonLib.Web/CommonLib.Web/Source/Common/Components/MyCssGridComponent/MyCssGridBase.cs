using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components.MyCssGridTemplateComponent;
using CommonLib.Web.Source.Common.Components.MyMediaQueryComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Models;
using Microsoft.AspNetCore.Components;
using Truncon.Collections;

namespace CommonLib.Web.Source.Common.Components.MyCssGridComponent
{
    public class MyCssGridBase : MyComponentBase
    {
        public DeviceSizeKind CurrentDeviceSize { get; set; }
        public DeviceSizeKind HighestDeviceSizeWithLayout { get; set; }
        public OrderedDictionary<DeviceSizeKind, CssGridLayout> GridLayouts { get; set; }

        [Parameter]
        public BlazorParameter<CssGridLayout> Template { get; set; }

        [Parameter]
        public BlazorParameter<CssGridLayout> SMTemplate { get; set; }
        
        [Parameter]
        public BlazorParameter<CssGridLayout> MDTemplate { get; set; }

        [Parameter]
        public BlazorParameter<CssGridLayout> LGTemplate { get; set; }

        [Parameter]
        public BlazorParameter<CssGridLayout> XLTemplate { get; set; }
        
        [Parameter]
        public BlazorParameter<string> Padding { get; set; }

        [Parameter]
        public BlazorParameter<string> Gap { get; set; }

        [Parameter]
        public BlazorParameter<string> ColumnsGap { get; set; }

        [Parameter]
        public BlazorParameter<string> RowsGap { get; set; }
        
        protected override async Task OnInitializedAsync()
        {
            GridLayouts = new OrderedDictionary<DeviceSizeKind, CssGridLayout>();
            await Task.CompletedTask;
        }

        protected override async Task OnParametersSetAsync()
        {
            if (FirstParamSetup)
            {
                SetMainAndUserDefinedClasses("my-css-grid");
                SetCustomAndUserDefinedStyles(new Dictionary<string, string> { ["opacity"] = "0" });
                SetUserDefinedAttributes();
            }

            if (Padding.HasChanged())
            {
                Padding.ParameterValue = Padding.V.NullifyIfNullOrWhiteSpace();
                AddOrUpdateStyle("padding", Padding.V);
            }

            if (Gap.HasChanged() || ColumnsGap.HasChanged() || RowsGap.HasChanged())
            {
                Gap.ParameterValue = Gap.V.NullifyIfNullOrWhiteSpace();
                ColumnsGap.ParameterValue = ColumnsGap.V.NullifyIfNullOrWhiteSpace();
                RowsGap.ParameterValue = RowsGap.V.NullifyIfNullOrWhiteSpace();

                var columnsGap = ColumnsGap.V ?? Gap.V;
                var rowsGap = RowsGap.V ?? Gap.V;

                AddOrUpdateStyle("column-gap", columnsGap);
                AddOrUpdateStyle("row-gap", rowsGap);
            }

            if (Template.HasChanged() && Template.HasValue())
            {
                Template.ParameterValue.DeviceSize = DeviceSizeKind.XS;
                GridLayouts[Template.V.DeviceSize] = Template.V;
            }

            if (SMTemplate.HasChanged() && SMTemplate.HasValue())
            {
                SMTemplate.ParameterValue.DeviceSize = DeviceSizeKind.SM;
                GridLayouts[SMTemplate.V.DeviceSize] = SMTemplate.V;
            }

            if (MDTemplate.HasChanged() && MDTemplate.HasValue())
            {
                MDTemplate.ParameterValue.DeviceSize = DeviceSizeKind.MD;
                GridLayouts[MDTemplate.V.DeviceSize] = MDTemplate.V;
            }

            if (LGTemplate.HasChanged() && LGTemplate.HasValue())
            {
                LGTemplate.ParameterValue.DeviceSize = DeviceSizeKind.LG;
                GridLayouts[LGTemplate.V.DeviceSize] = LGTemplate.V;
            }

            if (XLTemplate.HasChanged() && XLTemplate.HasValue())
            {
                XLTemplate.ParameterValue.DeviceSize = DeviceSizeKind.XL;
                GridLayouts[XLTemplate.V.DeviceSize] = XLTemplate.V;
            }

            await Task.CompletedTask;
        }

        protected override async Task OnAfterFirstRenderAsync()
        {
            var deviceSizeN = (await (await ModuleAsync).InvokeAndCatchCancellationAsync<string>("blazor_CssGridUtils_GetDeviceSizeAsync", StylesConfig.DeviceSizeKindNamesWithMediaQueries)).ToEnumN<DeviceSizeKind>();
            var deviceSize = deviceSizeN ?? throw new NullReferenceException("deviceSize shouldn't be null");
            await SetGridTemplateStylesAsync(deviceSize);
            await OnDeviceSizeChangingAsync(deviceSize);
        }

        protected async Task MediaQuery_ChangedAsync(MyMediaQueryChangedEventArgs e)
        {
            await SetGridTemplateStylesAsync(e.DeviceSize);
            await OnDeviceSizeChangingAsync(e.DeviceSize);
        }

        private async Task SetGridTemplateStylesAsync(DeviceSizeKind deviceSize)
        {
            CurrentDeviceSize = deviceSize;
            HighestDeviceSizeWithLayout = GridLayouts.Keys?.Cast<DeviceSizeKind?>().Where(d => d <= CurrentDeviceSize)?.MaxOrDefault() ?? throw new NullReferenceException($"Template for {CurrentDeviceSize.EnumToString()} or smaller device is not defined");
            var highestDefinedLayout = GridLayouts[HighestDeviceSizeWithLayout];
            Logger.For<MyCssGridBase>().Info($"deviceSize: {CurrentDeviceSize}, highestDeviceSizeWithDefinedLayout: {HighestDeviceSizeWithLayout}, Cols: {highestDefinedLayout.ColumnsLayout}, Rows: {highestDefinedLayout.RowsLayout}");
            AddOrUpdateStyle("grid-template-columns", highestDefinedLayout.ColumnsLayout);
            AddOrUpdateStyle("grid-template-rows", highestDefinedLayout.RowsLayout);
            RemoveStyle("opacity");
            await StateHasChangedAsync(true);
        }
        
        public event MyAsyncEventHandler<MyCssGridBase, MyMediaQueryChangedEventArgs> DeviceSizeChanged;
        private async Task OnDeviceSizeChangingAsync(MyMediaQueryChangedEventArgs e) => await DeviceSizeChanged.InvokeAsync(this, e);
        private async Task OnDeviceSizeChangingAsync(DeviceSizeKind deviceSize) => await OnDeviceSizeChangingAsync(new MyMediaQueryChangedEventArgs(deviceSize));
    }
}
