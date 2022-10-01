using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
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
        public static CssGridArea Auto => new();
        public static CssGridArea C1SpanAll => new(CGACP.C1, CGARP.Auto, CGAS.All);
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
        
        public CssGridArea(int? column, int? row = null, int columnSpan = 1, int rowSpan = 1)
        {
            Column = column;
            Row = row;
            ColumnSpan = columnSpan;
            RowSpan = rowSpan;
        }

        public CssGridArea(CGACP column = CGACP.Auto, CGARP row = CGARP.Auto, CGAS columnSpan = CGAS.Span1, CGAS rowSpan = CGAS.Span1)
        {
            Column = column == CGACP.Auto ? null : column.EnumToString().After("C").ToInt();
            Row = row == CGARP.Auto ? null : row.EnumToString().After("R").ToInt();
            ColumnSpan = columnSpan == CGAS.All ? -1 : columnSpan.EnumToString().After("Span").ToInt();;
            RowSpan = rowSpan == CGAS.All ? -1 : rowSpan.EnumToString().After("Span").ToInt();;
        }
    }

    public enum CGACP { Auto, C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12 } // CssGridAreaColumnPlacement
    public enum CGARP { Auto, R1, R2, R3, R4, R5, R6, R7, R8, R9, R10, R11, R12 } // CssGridAreaRowPlacement
    public enum CGAS { All, Span1, Span2, Span3, Span4, Span5, Span6, Span7, Span8, Span9, Span10, Span11, Span12 } // CssGridAreaSpan
   
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
