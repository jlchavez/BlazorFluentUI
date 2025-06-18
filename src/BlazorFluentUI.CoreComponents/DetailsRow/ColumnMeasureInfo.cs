using System;

namespace BlazorFluentUI
{
    public class ColumnMeasureInfo<TItem>
    {
        public int Index { get; set; }
        public IDetailsRowColumn<TItem>? Column { get; set; }
        public Action<double>? OnMeasureDone { get; set; }
    }
}
