using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace WindowsScreenTime.Models
{
    public partial class ProcessUsage : ObservableObject
    {
        [ObservableProperty]
        private BitmapImage? processIcon;

        [ObservableProperty]
        private string? processName;

        [ObservableProperty]
        private int usageTime;

        [ObservableProperty]
        private double ramUsagePer;

        [ObservableProperty]
        private long memorySize;

        [ObservableProperty]
        private string? executablePath;
    }
}
