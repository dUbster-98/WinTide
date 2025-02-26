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
using System.Drawing;

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

        private List<ProcessChartInfo> _data;
        [ObservableProperty]
        private Axis[] xAxes = [new Axis { SeparatorsPaint = new SolidColorPaint(new SKColor(220, 220, 220)) }];
        [ObservableProperty]
        private Axis[] yAxes = [new Axis { IsVisible = false }];
        [ObservableProperty]
        private ISeries[] _series;

        public bool IsReading { get; set; } = true;
        private ProcessChartInfo[] SortData() => [.. _data.OrderBy(x => x.Value)];
        // ..(spread 연산자)는 컬렉션의 모든 요소를 새 배열로 복사하는 역할

        public class ProcessChartInfo : ObservableValue
        {
            public ProcessChartInfo(string name, int value, SolidColorPaint paint, string iconPath)
            {
                
                Name = name;
                Paint = paint;
                IconPath = iconPath;
                // the ObservableValue.Value property is used by the chart
                Value = value;
            }

            public string Name { get; set; }
            public SolidColorPaint Paint { get; set; }
            public string? IconPath { get; set; } 
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
            PresetChange();
            _ = StartRace();           
        }

        private void Initialize()
        {
            Task _processMonitoringTask = Task.Run(() => MonitorActiveWindow());
            Task _autoSaveTask = Task.Run(() => AutoSave());     
        }

        private void UpdateProcessList()
        {
            var paints = Enumerable.Range(0, 7)
                                   .Select(i => new SolidColorPaint(ColorPalletes.MaterialDesign500[i].AsSKColor()))
                                   .ToArray();

            _data = new List<ProcessChartInfo>();

            int i = 0;
            foreach (var proc in ProcessList)
            {
                _data.Add(new(proc.EditedName, proc.UsageTime, paints[i], proc.IconPath));

                ++i;
                if (i == ProcessList.Count())
                    i = 0;
            }
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var projectRoot = Directory.GetParent(baseDirectory)?.Parent?.Parent?.Parent?.FullName;

            var fontPath = Path.Combine(projectRoot, "Resources/Fonts/Recipekorea FONT.ttf");
            var koreanTypeface = SKTypeface.FromFile(fontPath);
            var rowSeries = new RowSeries<ProcessChartInfo, MyBarGeometry>
            {
                Values = SortData(),
                DataLabelsPaint = new SolidColorPaint(new SKColor(245, 245, 245))
                {
                    SKTypeface = koreanTypeface // 한글 폰트 적용
                },
                DataLabelsPosition = DataLabelsPosition.End,
                DataLabelsTranslate = new(-1, 0),
                DataLabelsFormatter = point => $"{point.Model!.Name} {point.Coordinate.PrimaryValue}",
                MaxBarWidth = 100,
                Padding = 10,
            }
            .OnPointMeasured(point =>
            {
                // assign a different color to each point
                if (point.Visual is null) return;
                point.Visual.Fill = point.Model!.Paint;
                point.Visual.UpdateData(point.Model!);

            });

            Series = [rowSeries];
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

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private async void MonitorActiveWindow()
        {
            List<string> procInWindows = new();

            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd)) return true; // 보이지 않는 창은 무시

                GetWindowThreadProcessId(hWnd, out uint processId);
                Process process = Process.GetProcessById((int)processId);

                procInWindows.Add(process.ProcessName);

                var target = ProcessList.FirstOrDefault(p => p.ProcessName == process.ProcessName);
                if (target != null)
                {
                    target.UsageTime += 1;
                }

                return true; // 다음 창도 계속 탐색
            }, IntPtr.Zero);

            await Task.Delay(60000);
        }

        private async void AutoSave()
        {
            while (true)
            {
                await Task.Delay(60000);
                _databaseService.UpdateDataToDB("test", "1", DateTime.Now.ToString("yyyy-MM-dd"));
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
                        proc.UsageTime = 100;
                        if (processName != null)
                        {
                            if (proc.EditedName == null)
                                proc.EditedName = processName;

                            string iconPath = Path.Combine(ResourcePath, processName + ".png");
                            proc.IconPath = iconPath;
                            //if (!File.Exists(iconPath))
                            //    continue;
                            //else
                            //{
                            //    proc.ProcessIcon = new BitmapImage(new Uri(iconPath));
                            //}
                        }
                        ProcessList.Add(proc);
                    }
                }
            }

            UpdateProcessList();
        }

        [RelayCommand]
        private void TimerSet()
        {

        }
    }
}

            