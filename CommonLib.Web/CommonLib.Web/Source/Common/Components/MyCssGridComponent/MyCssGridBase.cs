using System;
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
                SetMainCustomAndUserDefinedClasses("my-css-grid", new[] { "my-d-none" });
                SetUserDefinedStyles();
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
            
            await Task.CompletedTask;
        }

        protected override async Task OnAfterFirstRenderAsync()
        {
            var deviceSize = (await (await ModuleAsync).InvokeAndCatchCancellationAsync<string>("blazor_CssGridUtils_GetDeviceSizeAsync", StylesConfig.DeviceSizeKindNamesWithMediaQueries)).ToEnumN<DeviceSizeKind>();
            await SetGridTemplateStylesAsync(deviceSize);
        }

        protected async Task MediaQuery_ChangedAsync(MyMediaQueryChangedEventArgs e)
        {
            await SetGridTemplateStylesAsync(e.DeviceSize);
        }

        private async Task SetGridTemplateStylesAsync(DeviceSizeKind? deviceSizeN)
        {
            CurrentDeviceSize = deviceSizeN ?? throw new NullReferenceException("deviceSize shouldn't be null");
            HighestDeviceSizeWithLayout = GridLayouts.Keys?.Where(d => d <= CurrentDeviceSize)?.Max() ?? throw new NullReferenceException("deviceSize shouldn't be null");
            var highestDefinedLayout = GridLayouts.VorN(CurrentDeviceSize) ?? GridLayouts[HighestDeviceSizeWithLayout];
            Logger.For<MyCssGridBase>().Info($"deviceSize: {CurrentDeviceSize}, highestDeviceSizeWithDefinedLayout: {HighestDeviceSizeWithLayout}, Cols: {highestDefinedLayout.ColumnsLayout}, Rows: {highestDefinedLayout.RowsLayout}");
            AddOrUpdateStyle("grid-template-columns", highestDefinedLayout.ColumnsLayout);
            AddOrUpdateStyle("grid-template-rows", highestDefinedLayout.RowsLayout);
            RemoveClass("my-d-none");
            await StateHasChangedAsync(true);
        }
    }
}
