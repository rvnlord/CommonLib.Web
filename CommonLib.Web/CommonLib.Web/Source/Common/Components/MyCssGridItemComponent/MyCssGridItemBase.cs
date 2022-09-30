﻿using System;
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
                AddOrUpdateStyle("grid-area", $"{highestDefinedArea.Row} / {highestDefinedArea.Column} / span {highestDefinedArea.RowSpan} / span {highestDefinedArea.ColumnSpan}");
            }
            else
                RemoveStyle("grid-area");

            RemoveStyle("opacity");
            await StateHasChangedAsync(true);
        }
    }

    public class CssGridArea
    {
        public DeviceSizeKind DeviceSize { get; set; } = DeviceSizeKind.XS;
        public int Column { get; set; }
        public int Row { get; set; }
        public int ColumnSpan { get; set; }
        public int RowSpan { get; set; }

        public CssGridArea() { }

        public CssGridArea(int column, int row, int columnSpan, int rowSpan)
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
