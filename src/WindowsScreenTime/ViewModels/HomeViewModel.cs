using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using WindowsScreenTime.Models;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Input;
using WindowsScreenTime.Views;

namespace WindowsScreenTime.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        [ObservableProperty]
        private DateTime? startDate;
        [ObservableProperty]
        private DateTime? endDate;

        public ObservableCollection<ProcessUsage> ProcessList { get; set; }
        private System.Timers.Timer monitoringTimer;
        private readonly double totalRam;

        public HomeViewModel() 
        {
            ProcessList = new ObservableCollection<ProcessUsage>();
            monitoringTimer = new System.Timers.Timer(1000);
            monitoringTimer.Elapsed += MonitorActiveWindow;
            monitoringTimer.Start();
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private void MonitorActiveWindow(object? sender, ElapsedEventArgs e)
        {
            IntPtr hWnd = GetForegroundWindow();
            GetWindowThreadProcessId(hWnd, out uint processId);

            Process activeProcess = Process.GetProcessById((int)processId);
            var target = ProcessList.FirstOrDefault(p => p.ProcessName == activeProcess.ProcessName);
            if (target != null)
            {
                target.UsageTime += 1;
            }
        }
        [RelayCommand]
        public void AddProcess(string processName)
        {
            if (!ProcessList.Any(p => p.ProcessName == processName))
            {
                ProcessList.Add(new ProcessUsage { ProcessName = processName, UsageTime = 0 });
            }
        }

        [RelayCommand]
        public void RemoveProcess(ProcessUsage processName)
        {
            ProcessList.Remove(processName);
        }

        [RelayCommand]
        private void SetPeriodDay()
        {
            StartDate = DateTime.Now.AddDays(-1);
            EndDate = DateTime.Now;
        }
        [RelayCommand]
        private void SetPeriodWeek()
        {
            StartDate = DateTime.Now.AddDays(-7);
            EndDate = DateTime.Now;
        }
        [RelayCommand]
        private void SetPeriodMonth()
        {
            StartDate = DateTime.Now.AddMonths(-1);
            EndDate = DateTime.Now;
        }
    }
}
