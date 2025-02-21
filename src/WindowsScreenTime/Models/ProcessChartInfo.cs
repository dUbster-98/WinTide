using LiveCharts.Defaults;
using LiveChartsCore.SkiaSharpView.Painting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core.Tokens;

namespace WindowsScreenTime.Models
{
    public class ProcessChartInfo : ObservableValue
    {
        public ProcessChartInfo(string name, int value, SolidColorPaint paint)
        {
            Name = name;
            Paint = paint;

            // the ObservableValue.Value property is used by the chart
            Value = value;
        }

        public string Name { get; set; }
        public SolidColorPaint Paint { get; set; }
    }
}
