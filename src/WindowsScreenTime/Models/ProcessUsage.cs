using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsScreenTime.Models
{
    public class ProcessUsage : ObservableObject
    {
        private string _processName;
        public string ProcessName
        {
            get => _processName; 
            set => SetProperty(ref _processName, value);
        }

        public int _usageTime;
        public int UsageTime
        {
            get => _usageTime;
            set => SetProperty(ref _usageTime, value);
        }
    }
}
