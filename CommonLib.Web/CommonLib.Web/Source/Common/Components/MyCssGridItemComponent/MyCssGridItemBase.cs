using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Web.Source.Common.Components.MyCssGridComponent;
using CommonLib.Web.Source.Common.Components.MyMediaQueryComponent;
using CommonLib.Web.Source.Models;
using Microsoft.AspNetCore.Components;
using Truncon.Collections;

namespace CommonLib.Web.Source.Common.Components.MyCssGridItemComponent
{
    public class MyCssGridItemBase : MyComponentBase
    {
        public DeviceSizeKind CurrentDeviceSize { get; set; }
        public DeviceSizeKind? HighestDeviceSizeWithArea { get; set; }
        public OrderedDictionary<DeviceSizeKind, CssGridArea> GridAreas { get; set; }

        [Parameter]
        public BlazorParameter<CssGridArea> Area { get; set; }

        [Parameter]
        public BlazorParameter<CssGridArea> SMArea { get; set; }
        
        [Parameter]
        public BlazorParameter<CssGridArea> MDArea { get; set; }

        [Parameter]
        public BlazorParameter<CssGridArea> LGArea { get; set; }

        [Parameter]
        public BlazorParameter<CssGridArea> XLArea { get; set; }

        [Parameter]
        public BlazorParameter<CssGridAreaHide> Hide { get; set; }

        protected override async Task OnInitializedAsync()
        {
            GridAreas = new OrderedDictionary<DeviceSizeKind, CssGridArea>();
            await Task.CompletedTask;
        }

        protected override void OnParametersSet()
        {
            if (FirstParamSetup)
            {
                SetMainAndUserDefinedClasses("my-css-grid-item");
                SetCustomAndUserDefinedStyles(new Dictionary<string, string> { ["opacity"] = "0" });
                SetUserDefinedAttributes();
                
                ((MyCssGridBase)Parent).DeviceSizeChanged -= DeviceSize_Changed;
                ((MyCssGridBase)Parent).DeviceSizeChanged += DeviceSize_Changed;
            }

            if (Area.HasChanged() && Area.HasValue())
            {
                Area.ParameterValue.DeviceSize = DeviceSizeKind.XS;
                GridAreas[Area.V.DeviceSize] = Area.V;
            }

            if (SMArea.HasChanged() && SMArea.HasValue())
            {
                SMArea.ParameterValue.DeviceSize = DeviceSizeKind.SM;
                GridAreas[SMArea.V.DeviceSize] = SMArea.V;
            }

            if (MDArea.HasChanged() && MDArea.HasValue())
            {
                MDArea.ParameterValue.DeviceSize = DeviceSizeKind.MD;
                GridAreas[MDArea.V.DeviceSize] = MDArea.V;
            }

            if (LGArea.HasChanged() && LGArea.HasValue())
            {
                LGArea.ParameterValue.DeviceSize = DeviceSizeKind.LG;
                GridAreas[LGArea.V.DeviceSize] = LGArea.V;
            }

            if (XLArea.HasChanged() && XLArea.HasValue())
            {
                XLArea.ParameterValue.DeviceSize = DeviceSizeKind.XL;
                GridAreas[XLArea.V.DeviceSize] = XLArea.V;
            }
        }
        
        private async Task DeviceSize_Changed(MyCssGridBase sender, MyMediaQueryChangedEventArgs e, CancellationToken token)
        {
            CurrentDeviceSize = e.DeviceSize;
            HighestDeviceSizeWithArea = GridAreas.Keys?.Cast<DeviceSizeKind?>().Where(d => d <= CurrentDeviceSize)?.MaxOrDefault();
            if (HighestDeviceSizeWithArea is not null) // area is not defined in any way
            {
                var highestDefinedArea = GridAreas[(DeviceSizeKind)HighestDeviceSizeWithArea];

                if (highestDefinedArea.Row is not null)
                    AddOrUpdateStyle("grid-row-start", highestDefinedArea.Row.ToString());
                else
                    RemoveStyle("grid-row-start");

                if (highestDefinedArea.Column is not null)
                    AddOrUpdateStyle("grid-column-start", highestDefinedArea.Column.ToString());
                else
                    RemoveStyle("grid-column-start");

                AddOrUpdateStyle("grid-row-end", highestDefinedArea.RowSpan > 0 ? $"span {highestDefinedArea.RowSpan}" : "-1");
                AddOrUpdateStyle("grid-column-end", highestDefinedArea.ColumnSpan > 0 ? $"span {highestDefinedArea.ColumnSpan}" : "-1");
            }
            else
                RemoveStyles(new[] { "grid-area", "grid-row-start", "grid-column-start", "grid-row-end", "grid-column-end" });

            RemoveStyle("opacity");
            await StateHasChangedAsync(true);
        }
    }

    public class CssGridArea
    {
        public DeviceSizeKind DeviceSize { get; set; } = DeviceSizeKind.XS;
        public int? Column { get; set; }
        public int? Row { get; set; }
        public int ColumnSpan { get; set; }
        public int RowSpan { get; set; }

        public CssGridArea()
        {
            Column = null;
            Row = null;
            ColumnSpan = 1;
            RowSpan = 1;
        }
        
        public CssGridArea(int? column = null, int? row = null, int columnSpan = 1, int rowSpan = 1)
        {
            Column = column;
            Row = row;
            ColumnSpan = columnSpan;
            RowSpan = rowSpan;
        }
    }

    public class CssGridAreaHide
    {
        public DeviceSizeKind? From { get; set; }
        public DeviceSizeKind? To { get; set; }

        public CssGridAreaHide() { }

        public CssGridAreaHide(DeviceSizeKind from)
        {
            From = from;
        }

        public CssGridAreaHide(DeviceSizeKind from, DeviceSizeKind to)
        {
            From = from;
            To = to;
        }
    }
}
