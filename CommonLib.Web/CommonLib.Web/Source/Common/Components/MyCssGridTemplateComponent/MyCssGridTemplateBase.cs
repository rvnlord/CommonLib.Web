using System;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Web.Source.Common.Components.MyCssGridComponent;
using CommonLib.Web.Source.Models;
using Microsoft.AspNetCore.Components;

namespace CommonLib.Web.Source.Common.Components.MyCssGridTemplateComponent
{
    public class MyCssGridTemplateBase : MyComponentBase
    {
        [Parameter]
        public BlazorParameter<DeviceSizeKind?> DeviceSize { get; set; }

        [Parameter]
        public BlazorParameter<string> ColumnsLayout { get; set; }

        [Parameter]
        public BlazorParameter<string> RowsLayout { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await Task.CompletedTask;
        }

        protected override async Task OnParametersSetAsync()
        {
            if (FirstParamSetup)
                SetMainAndUserDefinedClasses("my-css-grid-template");

            if (Parent is not MyCssGridBase cssGrid)
                throw new ArgumentException("Grid Template has to be defined within the Css Grid Component");

            if (DeviceSize.HasChanged())
                DeviceSize.ParameterValue = DeviceSize.V ?? DeviceSizeKind.XS;

            if (ColumnsLayout.HasChanged() || RowsLayout.HasChanged())
            {
                var deviceSize = DeviceSize.V ?? throw new NullReferenceException("DeviceSize Parameter Value can't be null");
                if (cssGrid.GridLayouts.VorN(deviceSize) is not null)
                    throw new ArgumentException($"Template for {deviceSize.EnumToString()} device size has already benn set in CssGrid Parameter");
                cssGrid.GridLayouts[deviceSize] = new CssGridLayout
                {
                    DeviceSize = deviceSize,
                    ColumnsLayout = ColumnsLayout.V,
                    RowsLayout = RowsLayout.V
                };
            }

            await Task.CompletedTask;
        }
    }

    public class CssGridLayout
    {
        public DeviceSizeKind DeviceSize { get; set; } = DeviceSizeKind.XS;
        public string ColumnsLayout { get; set; }
        public string RowsLayout { get; set; }

        public CssGridLayout() { }

        public CssGridLayout(string columnsLayout, string rowsLayout)
        {
            ColumnsLayout = columnsLayout;
            RowsLayout = rowsLayout;
        }
    }
}
