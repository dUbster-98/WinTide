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
    public class ProcessUsage : ObservableObject
    {

        public BitmapImage? ProcessIcon { get; set; }
        public string? IconPath { get; set; } 
        public string? BaseName { get; set; }
        public string? ProcessName { get; set; }
        public string? EditedName { get; set; }
        public int PastUsage { get; set; }
        public int TodayUsage { get; set; }
        public double RamUsagePer { get; set; }
        public long MemorySize { get; set; }
        public string? ExecutablePath { get; set; }
    }
}
