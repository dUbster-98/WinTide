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
using System.IO;
using System.Windows.Input;
using WindowsScreenTime.Views;
using System.Windows.Controls;
using WindowsScreenTime.Services;
using System.Windows.Media.Imaging;
using YamlDotNet.Core.Tokens;
using System.Drawing.Text;
using LiveChartsCore;
using LiveChartsCore.Kernel.Events;
using LiveChartsCore.Defaults;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.Themes;
using SkiaSharp;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;

namespace WindowsScreenTime.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        private readonly IXmlSetService _xmlSetService;
        private readonly IProcessContainService _processContainService;
        private readonly IDatabaseService _databaseService;

        [ObservableProperty]
        private DateTime? startDate;
        [ObservableProperty]
        private DateTime? endDate;
        [ObservableProperty]
        private int? selectedPreset;

        public ObservableCollection<ProcessUsage> ProcessList { get; set; }

        private static string ResourcePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources/Icons");

        private readonly Random _r = new();
        private ProcessChartInfo[] _data;
        [ObservableProperty]
        private Axis[] xAxes = [new Axis { SeparatorsPaint = new SolidColorPaint(new SKColor(220, 220, 220)) }];
        [ObservableProperty]
        private Axis[] yAxes = [new Axis { IsVisible = false }];
        [ObservableProperty]
        private ISeries[] _series;

        public bool IsReading { get; set; } = true;
        private ProcessChartInfo[] SortData() => [.. _data.OrderBy(x => x.Value)];

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

        public HomeViewModel(IXmlSetService xmlSetService, IProcessContainService processContainService, IDatabaseService databaseService) 
        {
            _xmlSetService = xmlSetService;
            _processContainService = processContainService;
            _databaseService = databaseService;

            ProcessList = new ();

            if (_xmlSetService.LoadSelectedPreset() == null || _xmlSetService.LoadSelectedPreset() == "")
                SelectedPreset = 0;
            else
            {
                SelectedPreset = Convert.ToInt32(_xmlSetService.LoadSelectedPreset());
                PresetChange();
            }

            Initialize();
            BeginChartRace();
        }

        private void Initialize()
        {
            Task _processMonitoringTask = Task.Run(() => MonitorActiveWindow());
            Task _autoSaveTask = Task.Run(() => AutoSave());
        }

        private void BeginChartRace()
        {
            var paints = Enumerable.Range(0, 7)
            .Select(i => new SolidColorPaint(ColorPalletes.MaterialDesign500[i].AsSKColor()))
            .ToArray();

            _data =
            [
                new("Tsunoda",   500,  paints[0]),
                new("Sainz",     450,  paints[1]),
                new("Riccardo",  520,  paints[2]),
                new("Bottas",    550,  paints[3]),
                new("Perez",     660,  paints[4]),
                new("Verstapen", 920,  paints[5]),
                new("Hamilton",  1000, paints[6])
            ];

            var rowSeries = new RowSeries<ProcessChartInfo>
            {
                Values = SortData(),
                DataLabelsPaint = new SolidColorPaint(new SKColor(245, 245, 245)),
                DataLabelsPosition = DataLabelsPosition.Right,
                DataLabelsTranslate = new(-1, 0),
                DataLabelsFormatter = point => $"{point.Model!.Name} {point.Coordinate.PrimaryValue}",
                MaxBarWidth = 50,
                Padding = 10,
                Name= "F1 Drivers"
            }
            .OnPointMeasured(point =>
            {
                // assign a different color to each point
                if (point.Visual is null) return;
                point.Visual.Fill = point.Model!.Paint;
            });

            _series = [rowSeries];

            _ = StartRace();
        }

        public async Task StartRace()
        {
            await Task.Delay(1000);

            while (IsReading)
            {
                Series[0].Values = SortData();

                await Task.Delay(1000);
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private async void MonitorActiveWindow()
        {
            IntPtr hWnd = GetForegroundWindow();
            GetWindowThreadProcessId(hWnd, out uint processId);

            Process activeProcess = Process.GetProcessById((int)processId);
            var target = ProcessList.FirstOrDefault(p => p.ProcessName == activeProcess.ProcessName);
            if (target != null)
            {
                target.UsageTime += 1;
            }

            await Task.Delay(1000);
        }
        private async void AutoSave()
        {
            while (true)
            {
                await Task.Delay(60000);
                //_databaseService.UpdateDataToDB("test", "1", DateTime.Now.ToString("yyyy-MM-dd"));
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
                _xmlSetService.SaveSelectedPreset(SelectedPreset.ToString()!);

                List<ProcessUsage> processes = new();
                ProcessList.Clear();
                processes = _xmlSetService.LoadPresetProcess(SelectedPreset.ToString()!);

                if (processes != null)
                {
                    foreach (ProcessUsage proc in processes)
                    {
                        string? processName = proc.ProcessName;
                        if (processName != null)
                        {
                            if (proc.EditedName == null)
                                proc.EditedName = processName;
                           
                            string iconPath = Path.Combine(ResourcePath, processName + ".png");
                            if (!File.Exists(iconPath))
                                continue;
                            else
                            {
                                proc.ProcessIcon = new BitmapImage(new Uri(iconPath));
                            }
                        }
                        ProcessList.Add(proc);
                    }
                }
            }
        }
        [RelayCommand]
        private void TimerSet()
        {

        }
    }
}

            