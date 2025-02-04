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
using System.Windows.Controls;
using WindowsScreenTime.Services;
using System.Windows.Media.Imaging;

namespace WindowsScreenTime.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        private readonly IIniSetService _iniSetService;
        private readonly IXmlSetService _xmlSetService;
        private readonly IProcessContainService _processContainService;

        [ObservableProperty]
        private DateTime? startDate;
        [ObservableProperty]
        private DateTime? endDate;
        [ObservableProperty]
        private int selectedPreset;

        public ObservableCollection<ProcessUsage> ProcessList { get; set; }

        private System.Timers.Timer monitoringTimer;
        private readonly double totalRam;

        public HomeViewModel(IXmlSetService xmlSetService, IProcessContainService processContainService) 
        {
            _xmlSetService = xmlSetService;
            _processContainService = processContainService;

            ProcessList = new ();
            monitoringTimer = new System.Timers.Timer(1000);
            monitoringTimer.Elapsed += MonitorActiveWindow;
            monitoringTimer.Start();

            if (_xmlSetService.LoadSelectedPreset() == null || _xmlSetService.LoadSelectedPreset() == "")
                SelectedPreset = 0;
            else
            {
                SelectedPreset = Convert.ToInt32(_xmlSetService.LoadSelectedPreset());
                PresetChange();
            }                
        }

        private void Initialize()
        {

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
                ProcessList.Add(new ProcessUsage { ProcessName = processName, UsageTime = "0" });
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
        [RelayCommand]
        private void PresetChange()
        {
            if (SelectedPreset != null)
            {
                _xmlSetService.SaveSelectedPreset(SelectedPreset.ToString());

                List<ProcessUsage> processes = new();
                ProcessList.Clear();
                processes = _xmlSetService.LoadPresetProcess(SelectedPreset.ToString());

                if (processes != null)
                {
                    foreach (ProcessUsage proc in processes)
                    {
                        string? processName = proc.ProcessName;

                        if (processName != null)
                        {
                            string? path = _processContainService.GetProcessPathByString(processName);

                            if (path != null)
                            {
                                BitmapImage? icon = _processContainService.GetProcessIcon(path);
                                proc.ProcessIcon = icon;
                            }
                        }
                        ProcessList.Add(proc);
                    }
                }
            }
        }
    }
}

            