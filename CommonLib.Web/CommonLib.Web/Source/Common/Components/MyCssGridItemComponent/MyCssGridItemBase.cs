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
        public OrderedDictionary<DeviceSizeKind, CssGridAreaGap> GridAreaGaps { get; set; }

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

        [Parameter]
        public BlazorParameter<CssGridAreaGap> Gap { get; set; }

        [Parameter]
        public BlazorParameter<CssGridAreaGap> SMGap { get; set; }
        
        [Parameter]
        public BlazorParameter<CssGridAreaGap> MDGap { get; set; }

        [Parameter]
        public BlazorParameter<CssGridAreaGap> LGGap { get; set; }

        [Parameter]
        public BlazorParameter<CssGridAreaGap> XLGap { get; set; }

        protected override async Task OnInitializedAsync()
        {
            GridAreas = new OrderedDictionary<DeviceSizeKind, CssGridArea>();
            GridAreaGaps = new OrderedDictionary<DeviceSizeKind, CssGridAreaGap>();
            await Task.CompletedTask;
        }

        protected override async Task OnParametersSetAsync()
        {
            if (FirstParamSetup)
            {
                SetMainAndUserDefinedClasses("my-css-grid-item");
                SetUserDefinedStyles();
                //SetCustomAndUserDefinedStyles(new Dictionary<string, string> { ["opacity"] = "0" });
                SetUserDefinedAttributes();
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

            if (Gap.HasChanged() && Gap.HasValue())
                GridAreaGaps[DeviceSizeKind.XS] = Gap.V;
            if (SMGap.HasChanged() && SMGap.HasValue())
                GridAreaGaps[DeviceSizeKind.SM] = SMGap.V;
            if (MDGap.HasChanged() && MDGap.HasValue())
                GridAreaGaps[DeviceSizeKind.MD] = MDGap.V;
            if (LGGap.HasChanged() && LGGap.HasValue())
                GridAreaGaps[DeviceSizeKind.LG] = LGGap.V;
            if (XLGap.HasChanged() && XLGap.HasValue())
                GridAreaGaps[DeviceSizeKind.XL] = XLGap.V;

            if (FirstParamSetup)
                await SetGridItemStylesAsync(DeviceSizeKind.XL, false);
        }

        protected override async Task OnLayoutAfterRenderFinishedAsync(Guid sessionId, DeviceSizeKind deviceSize)
        {
            await SetGridItemStylesAsync(deviceSize);
        }

        protected override async Task OnDeviceSizeChangedAsync(DeviceSizeKind deviceSize)
        {
            await SetGridItemStylesAsync(deviceSize);
        }

        private async Task SetGridItemStylesAsync(DeviceSizeKind deviceSize, bool refresh = true)
        {
            CurrentDeviceSize = deviceSize;
            HighestDeviceSizeWithArea = GridAreas.Keys?.Cast<DeviceSizeKind?>().Where(d => d <= CurrentDeviceSize)?.MaxOrDefault();
            if (HighestDeviceSizeWithArea is not null) // area is not defined in any way
            {
                var highestDefinedArea = GridAreas[(DeviceSizeKind)HighestDeviceSizeWithArea];

                if (highestDefinedArea.Row is not null)
                    AddStyle("grid-row-start", highestDefinedArea.Row.ToString());
                else
                    RemoveStyle("grid-row-start");

                if (highestDefinedArea.Column is not null)
                    AddStyle("grid-column-start", highestDefinedArea.Column.ToString());
                else
                    RemoveStyle("grid-column-start");

                AddStyle("grid-row-end", highestDefinedArea.RowSpan > 0 ? $"span {highestDefinedArea.RowSpan}" : "-1");
                AddStyle("grid-column-end", highestDefinedArea.ColumnSpan > 0 ? $"span {highestDefinedArea.ColumnSpan}" : "-1");
            }
            else
                RemoveStyles(new[] { "grid-area", "grid-row-start", "grid-column-start", "grid-row-end", "grid-column-end" });

            var highestDeviceSizeWithAreaGap = GridAreaGaps.Keys?.Cast<DeviceSizeKind?>().Where(d => d <= CurrentDeviceSize)?.MaxOrDefault();
            if (highestDeviceSizeWithAreaGap is not null)
            {
                var highestDefinedGap = GridAreaGaps[(DeviceSizeKind)highestDeviceSizeWithAreaGap];

                if (!highestDefinedGap.Top.IsNullOrWhiteSpace())
                    AddStyle("margin-top", highestDefinedGap.Top);
                else
                    RemoveStyle("margin-top");

                if (!highestDefinedGap.Right.IsNullOrWhiteSpace())
                    AddStyle("margin-right", highestDefinedGap.Right);
                else
                    RemoveStyle("margin-right");

                if (!highestDefinedGap.Bottom.IsNullOrWhiteSpace())
                    AddStyle("margin-bottom", highestDefinedGap.Bottom);
                else
                    RemoveStyle("margin-bottom");

                if (!highestDefinedGap.Left.IsNullOrWhiteSpace())
                    AddStyle("margin-left", highestDefinedGap.Left);
                else
                    RemoveStyle("margin-left");
            }
            else
                RemoveStyles(new[] { "margin-top", "margin-right", "margin-bottom", "margin-left" });

            //RemoveStyle("opacity");
            if (refresh)
                await StateHasChangedAsync(true);
        }
    }

    public class CssGridArea
    {
        public static CssGridArea Auto => new();
        public static CssGridArea C1 => new(CGACP.C1);
        public static CssGridArea C2 => new(CGACP.C2);
        public static CssGridArea C3 => new(CGACP.C3);
        public static CssGridArea C4 => new(CGACP.C4);
        public static CssGridArea C1SpanAll => new(CGACP.C1, CGARP.Auto, CGAS.All);
        public static CssGridArea C2SpanAll => new(CGACP.C2, CGARP.Auto, CGAS.All);
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

    public class CssGridAreaGap
    {
        public static CssGridAreaGap Auto => new();
        
        public string Top { get; set; }
        public string Right { get; set; }
        public string Bottom { get; set; }
        public string Left { get; set; }

        public static CssGridAreaGap OnlyNegatedDefaultBottom => new(null, null, (-StylesConfig.Gutter).Px());

        public CssGridAreaGap() { }

        public CssGridAreaGap(string top, string right = null, string bottom = null, string left = null)
        {
            Top = top;
            Right = right;
            Bottom = bottom;
            Left = left;
        }

        public static CssGridAreaGap OnlyTop(string top) => new(top);
        public static CssGridAreaGap OnlyRight(string right) => new(null, right);
        public static CssGridAreaGap OnlyBottom(string bottom) => new(null, null, bottom);
        public static CssGridAreaGap OnlyLeft(string left) => new(null, null, null, left);
    }
}
