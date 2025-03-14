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
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using Newtonsoft.Json.Linq;
using Hardcodet.Wpf.TaskbarNotification.Interop;
using LiveCharts;
using LiveChartsCore.Kernel;

namespace WindowsScreenTime.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        private readonly IXmlSetService _xmlSetService;
        private readonly IProcessContainService _processContainService;
        private readonly IDatabaseService _databaseService;

        [ObservableProperty]
        private DateTime? startDate;
        partial void OnStartDateChanged(DateTime? value)
        {
            if (startDate != null && endDate != null)
            {
                PresetChange();
            }
        }
        [ObservableProperty]
        private DateTime? endDate;
        partial void OnEndDateChanged(DateTime? value)
        {
            if (endDate != null && endDate != null)
            {
                PresetChange();
            }
        }

        public List<string> MinList { get; } = new List<string> { "00", "10", "20", "30", "40", "50" };

        [ObservableProperty]
        private string alarmHours;
        partial void OnAlarmHoursChanged(string value)
        {
            if (int.TryParse(value, out int hours))
            {
                if (hours < 0)
                    alarmHours = "0";

                else
                    alarmHours = value;
            }
            else
                alarmHours = "1"; // 잘못된 입력이 들어오면 기본값 설정
        }

        [ObservableProperty]
        private string alarmMinutes;
        [ObservableProperty]
        private string remainingTime = "00:00:00";
        private int remainingSeconds;
        private string AlarmMessage;
        private DispatcherTimer timer;
        private void StartTimer()
        {
            if (timer == null)
            {
                timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                timer.Tick += Timer_Tick;
            }
            timer.Start();
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (remainingSeconds > 0)
            {
                remainingSeconds--;
                UpdateRemainingTime();
            }
            else
            {
                timer.Stop();

                AlarmPopup alarmPopup = new();

                var message = new TransferAlarmMessage { Message = AlarmMessage };
                WeakReferenceMessenger.Default.Send(message);

                alarmPopup.Show();
            }
        }
        private void UpdateRemainingTime()
        {
            TimeSpan time = TimeSpan.FromSeconds(remainingSeconds);
            RemainingTime = $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
        }
        [ObservableProperty]
        private bool isSystemOff = false;

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
        private readonly Random _r = new();

        private int counter = 60000;
        private CancellationTokenSource cts = new();

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

            startDate = DateTime.Today.AddDays(-7);
            endDate = DateTime.Today;
            AlarmHours = "1";
            AlarmMinutes = "00";

            WeakReferenceMessenger.Default.Register<TransferViewModelActivation>(this, OnTransferViewModelState);

            ProcessList = new();
            _databaseService.InitializeDataBase();

            if (_xmlSetService.LoadSelectedPreset() == null || _xmlSetService.LoadSelectedPreset() == "")
                SelectedPreset = 0;
            else
            {
                SelectedPreset = Convert.ToInt32(_xmlSetService.LoadSelectedPreset());
                PresetChange();
            }

            Initialize();
        }

        private void Initialize()
        {
            Task _processMonitoringTask = Task.Run(() => MonitorActiveWindow());
        }
        private void OnTransferViewModelState(object recipient, TransferViewModelActivation message)
        {
            if (_xmlSetService.LoadSelectedPreset() == null || _xmlSetService.LoadSelectedPreset() == "")
                SelectedPreset = 0;
            else
            {
                SelectedPreset = Convert.ToInt32(_xmlSetService.LoadSelectedPreset());
                PresetChange();
            }
        }

        private void UpdateProcessList()
        {
            var paints = Enumerable.Range(0, 7)
                                   .Select(i => new SolidColorPaint(ColorPalletes.MaterialDesign500[i].AsSKColor()))
                                   .ToArray();

            _data = new List<ProcessChartInfo>();
            IsReading = false;

            int i = 0;
            var value = 0;
            foreach (var proc in ProcessList)
            {
                if (EndDate?.ToString() == DateTime.Today.ToString())
                {
                    value = proc.PastUsage + proc.TodayUsage;
                }
                else
                {
                    value = proc.PastUsage;
                }

                switch (counter)
                {
                    case 1:
                        value = value * 5;
                        break;
                    case 12:
                        value = value / 12;
                        break;
                    case 720:
                        value = value / 720;
                        break;
                    default:
                        value = value / 12; 
                        break;
                }

                _data.Add(new(proc.EditedName, value, paints[i], proc.IconPath));

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
                MiniatureShapeSize = 20,
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

        public async Task GetWindowProcess()
        {
            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd)) return true; // 보이지 않는 창은 무시
                int length = GetWindowTextLength(hWnd);
                if (length == 0) return true; // 창 제목이 없는 경우 무시
                if (IsIconic(hWnd)) return true; // 최소화된 창 무시

                int style = GetWindowLong(hWnd, GWL_STYLE);
                int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);

                // 툴 윈도우는 제외 (작업 표시줄 아이콘이 없는 창)
                if ((exStyle & WS_EX_TOOLWINDOW) != 0)
                {
                    return true;
                }

                GetWindowThreadProcessId(hWnd, out uint processId);
                Process process = Process.GetProcessById((int)processId);

                var target = ProcessList.FirstOrDefault(p => p.ProcessName == process.ProcessName);
                if (target != null)
                {
                    target.TodayUsage += 1;

                    //AutoSave
                    _databaseService.UpdateDataToDB(target.ProcessName, target.TodayUsage, DateTime.Now.ToString("yyyy-MM-dd"));
                }

                return true; // 다음 창도 계속 탐색
            }, IntPtr.Zero);
        }

        public async Task SetProcessRanking()
        {
            foreach (var proc in ProcessList)
            {
                var value = 0;

                var item = _data.FirstOrDefault(p => p.Name == proc.EditedName);
                if (item != null)
                {
                    if (EndDate.Value == DateTime.Today)
                    {
                        value = proc.PastUsage + proc.TodayUsage;
                    }
                    else
                    {
                        value = proc.PastUsage;
                    }

                    switch (counter)
                    {
                        case 1:
                            value = value * 5;
                            break;
                        case 12:
                            value = value / 12;
                            break;
                        case 720:
                            value = value / 720;
                            break;
                        default:
                            value = value / 12;
                            break;
                    }

                    item.Value = value;
                }            
            }
            Series[0].Values = SortData();
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

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd); // 창이 최소화되었는지 확인

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;
        private const int WS_VISIBLE = 0x10000000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_APPWINDOW = 0x00040000;

        private async Task MonitorActiveWindow()
        {
            using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

            while (await timer.WaitForNextTickAsync()) // 5초마다 실행
            {
                //var token = cts.Token;
                //try
                //{
                //    await Task.Delay(5000, token);
                //}
                //catch (TaskCanceledException)
                //{
                //    _ = MonitorActiveWindow();
                //    break;
                //}

                _ = GetWindowProcess();

                _ = SetProcessRanking();
            }
        }

        [RelayCommand]
        public void AddProcess(string processName)
        {
            if (!ProcessList.Any(p => p.ProcessName == processName))
            {
                ProcessList.Add(new ProcessUsage { ProcessName = processName, TodayUsage = 0 });
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
                        proc.IconPath = iconPath;
                        //if (!File.Exists(iconPath))
                        //    continue;
                        //else
                        //{
                        //    proc.ProcessIcon = new BitmapImage(new Uri(iconPath));
                        //}
                    }
                    ProcessList.Add(proc);
                    _databaseService.WriteDataToDB(proc.ProcessName, DateTime.Today.ToString("yyyy-MM-dd"));
                }
            }

            DateTime yesterDay = EndDate.Value.AddDays(-1);
            foreach (var proc in ProcessList)
            {
                proc.PastUsage = _databaseService.QueryPastUsageTime(proc.ProcessName, StartDate.ToString(), yesterDay.ToString());
                proc.TodayUsage = _databaseService.QueryTodayUsageTime(proc.ProcessName, DateTime.Today.ToString("yyyy-MM-dd"));
            }

            UpdateProcessList();
        }

        [RelayCommand]
        private void TimerSet()
        {
            if (int.TryParse(AlarmHours, out int hours) && int.TryParse(AlarmMinutes, out int minutes))
            {
                int hoursToseconds = hours * 3600; // 시간을 초 단위로 변환
                int minutesToSeconds = minutes * 60; // 분을 초 단위로 변환
                remainingSeconds = hoursToseconds + minutesToSeconds;
                AlarmMessage = $"{hours:D2}시간 {minutes:D2}분 지났습니다.";
                StartTimer();

                if (IsSystemOff)
                {
                    string shutdownCmd = $"/C shutdown -s -t {remainingSeconds}";
                    Process.Start(new ProcessStartInfo("cmd.exe", shutdownCmd)
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                }
            }
        }
        [RelayCommand]
        private void TimerUnset()
        {
            remainingSeconds = 0;

            if (IsSystemOff)
            {
                Process.Start(new ProcessStartInfo("cmd.exe", "/C shutdown -a")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            }
        }

        [RelayCommand]
        private void SetSecond()
        {
            //cts.Cancel();
            //cts = new CancellationTokenSource();
            counter = 1;
            UpdateProcessList();
        }
        [RelayCommand]
        private void SetMinute()
        {
            counter = 12;
            UpdateProcessList();
        }
        [RelayCommand]
        private void SetHour()
        {
            counter = 720;
            UpdateProcessList();
        }
        [RelayCommand]
        private void BarClick(LiveChartsCore.Kernel.Events.VisualElementsEventArgs args)
        {
            if (args == null) return;

            // VisualElementsEventArgs에서 필요한 정보 추출
            var chartPoint = args.PointerLocation; // ChartPoint 정보가 이 안에 있을 수 있음
            var chart = args.Chart;
        }
        [RelayCommand]
        private void Back()
        {

        }
    }
}

            