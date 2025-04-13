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
using OpenTK.Platform;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView.WPF;
using LiveChartsCore.Drawing;
using System.Windows.Documents;
using WpfAnimatedGif;
using WindowsScreenTime.Themes;

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
        [ObservableProperty]
        private bool secChecked = false;
        [ObservableProperty]
        private bool minChecked = true;
        [ObservableProperty]
        private bool hourChecked = false;
        [ObservableProperty]
        private bool timeEnable = true;
        [ObservableProperty]
        private bool presetEnable = true;

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
        private DateTime pgOnDate;
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
        public ObservableCollection<ProcessUsage> ProcessList { get; set; } = [];
        public List<ProcessUsage> EntireProcessList { get; set; } = [];
        private static string ResourcePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources/Icons");

        private List<ProcessChartInfo> _data;
        [ObservableProperty]
        private Axis[] xAxes = [new Axis { SeparatorsPaint = new SolidColorPaint(new SKColor(220, 220, 220)), MinLimit = 0 }];
        [ObservableProperty]
        private Axis[] yAxes = [new Axis { IsVisible = false }];
        [ObservableProperty]
        private ISeries[] _series;
        [ObservableProperty]
        private Axis[] xAxes2 = [new DateTimeAxis(TimeSpan.FromDays(1), date => date.ToString("MM-dd"))];
        [ObservableProperty]
        private Axis[] yAxes2 = [new Axis()];
        private SKColor foreGround = new();

        [ObservableProperty]
        private ISeries[] _series2;

        [ObservableProperty]
        private bool isChart1Visible = true;
        [ObservableProperty]
        private bool isChart2Visible = false;
        [ObservableProperty]
        private bool isGifVisible = false;

        private readonly HashSet<LiveChartsCore.Kernel.ChartPoint> _activePoints = [];
        public FindingStrategy Strategy { get; } = FindingStrategy.ExactMatch;

        private readonly Random _r = new();

        private int counter = 60000;
        private CancellationTokenSource cts = new();

        [ObservableProperty]
        private System.Windows.Controls.Image gifSource = new();
      
        private ProcessChartInfo[] SortData() => [.. _data.OrderBy(x => x.Value)];
        // ..(spread 연산자)는 컬렉션의 모든 요소를 새 배열로 복사하는 역할

        public class ProcessChartInfo : ObservableValue
        {
            public ProcessChartInfo(string name, string editedName, int value, SolidColorPaint paint, string iconPath)
            {
                Name = name;
                EditedName = editedName;
                Paint = paint;
                IconPath = iconPath;
                // the ObservableValue.Value property is used by the chart
                Value = value;
            }

            public string Name { get; set; }
            public string EditedName { get; set; }
            public SolidColorPaint Paint { get; set; }
            public string? IconPath { get; set; }
        }

        public HomeViewModel(IXmlSetService xmlSetService, IProcessContainService processContainService, IDatabaseService databaseService)
        {
            _xmlSetService = xmlSetService;
            _processContainService = processContainService;
            _databaseService = databaseService;

            if (_xmlSetService.LoadConfig("DarkTheme") == true)           
                ThemeManager.ChangeTheme(ThemeType.Dark);
            if (_xmlSetService.LoadConfig("GifShow") == true)
                IsGifVisible = true;

            startDate = DateTime.Today.AddDays(-7);
            endDate = DateTime.Today;
            AlarmHours = "1";
            AlarmMinutes = "00";
            pgOnDate = DateTime.Today;

            WeakReferenceMessenger.Default.Register<TransferIsGifShowChange>(this, OnTransferIsGifShowChange);

            ProcessList = new();

            _databaseService.InitializeDataBase();

            _ = MonitorActiveWindow();
        }

        public void OnNavigatedTo()
        {
            if (_xmlSetService.LoadSelectedPreset() == null || _xmlSetService.LoadSelectedPreset() == "")
                SelectedPreset = 0;
            else
            {
                SelectedPreset = Convert.ToInt32(_xmlSetService.LoadSelectedPreset());
                PresetChange();
            }
        }

        public void OnNavigatedFrom()
        {

        }

        private void OnTransferIsGifShowChange(object recipient, TransferIsGifShowChange message)
        {
            if (message.isVisible == true)
                IsGifVisible = true;
            else
                IsGifVisible = false;
        }

        private void UpdateProcessList()
        {
            var paints = Enumerable.Range(0, 9)
                                   .Select(i => new SolidColorPaint(ColorPalletes.MaterialDesign500[i].AsSKColor()))
                                   .ToArray();

            _data = new List<ProcessChartInfo>();

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
                
                _data.Add(new(proc.ProcessName, proc.EditedName, value, paints[i], proc.IconPath));
                
                ++i;
                if (i == 9)
                    i = 0;
            }

            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var projectRoot = Directory.GetParent(baseDirectory)?.Parent?.Parent?.Parent?.FullName;

            var fontPath = Path.Combine(projectRoot, "Resources/Fonts/Recipekorea FONT.ttf");
            var koreanTypeface = SKTypeface.FromFile(fontPath);

            if (_xmlSetService.LoadConfig("DarkTheme") == true)
                foreGround = new SKColor(245, 245, 245);
            else
                foreGround = new SKColor(25, 25, 25);

            var rowSeries = new RowSeries<ProcessChartInfo, MyBarGeometry>
            {
                Values = SortData(),
                DataLabelsPaint = new SolidColorPaint(foreGround)
                {
                    SKTypeface = koreanTypeface // 한글 폰트 적용
                },
                DataLabelsPosition = DataLabelsPosition.End,
                DataLabelsTranslate = new(-1, 0),
                DataLabelsFormatter = point => $"{point.Model!.EditedName} {point.Coordinate.PrimaryValue}",
                DataLabelsPadding = new(45, 0, 10, 0),
                DataLabelsMaxWidth = 250,
                MaxBarWidth = 100,
                MiniatureShapeSize = 20,
                Padding = 5,
            }
            .OnPointMeasured(point =>
            {
                if (point.Visual is null) return;

                point.Visual.Fill = point.Model!.Paint;

                point.Visual.UpdateData(point.Model!);

                if (point.Visual.Width < 110 && point.Visual.Width != 0)
                {
                    point.Label.TranslateTransform = new(1, 0);
                    //point.Label.X = point.Visual.Width + 112;
                }
            });

            Series = [rowSeries];
        }
        private bool labelsAdjusted = false;

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

        [DllImport("user32.dll")]
        static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32.dll")]
        static extern IntPtr ChildWindowFromPointEx(IntPtr hwndParent, POINT Point, uint uFlags);

        const uint CWP_SKIPINVISIBLE = 0x0001; // 보이지 않는 창 무시
        const uint CWP_SKIPTRANSPARENT = 0x0002; // 투명 창 무시
        const uint CWP_SKIPDISABLED = 0x0004; // 비활성 창 무시
        const uint GA_ROOTOWNER = 3;


        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;
        private const int WS_VISIBLE = 0x10000000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_APPWINDOW = 0x00040000;

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
                if ((exStyle & WS_EX_TOOLWINDOW) != 0) return true;

                GetWindowThreadProcessId(hWnd, out uint processId);
                Process process = new();
                string displayName;
                try
                {
                    process = Process.GetProcessById((int)processId);
                    string processDescription = process.MainModule.FileVersionInfo.FileDescription; // 작업 관리자 이름 = 파일 설명
                    displayName = string.IsNullOrEmpty(processDescription) ? process.ProcessName : processDescription;
                }
                catch
                {
                    displayName = process.ProcessName;
                }

                if (GetWindowRect(hWnd, out RECT rect))
                {
                    POINT centerPoint = new POINT(
                        (rect.Left + rect.Right) / 2,
                        (rect.Top + rect.Bottom) / 2
                    );

                    // 윈도우의 중심점이 가려져 있는지 확인
                    IntPtr windowAtPoint = WindowFromPoint(centerPoint);
                    IntPtr childWindow = ChildWindowFromPointEx(windowAtPoint, centerPoint,
                        CWP_SKIPINVISIBLE | CWP_SKIPTRANSPARENT | CWP_SKIPDISABLED);
                    IntPtr topLevelWindow = GetAncestor(windowAtPoint, GA_ROOTOWNER);

                    bool isVisible = (topLevelWindow == hWnd || windowAtPoint == hWnd || childWindow == hWnd);

                    // 윈도우의 중심이 현재 화면의 중심과 같다면 보이는것
                    if (isVisible)
                    {
                        var target = ProcessList.FirstOrDefault(p => p.ProcessName == displayName);
                        if (target != null)
                        {
                            target.TodayUsage += 1;

                            //AutoSave
                            _databaseService.UpdateDataToDB(target.ProcessName, target.TodayUsage, DateTime.Today.ToString("yyyy-MM-dd"));
                        }
                    }
                }

                return true; // 다음 창도 계속 탐색
            }, IntPtr.Zero);
        }

        public async Task SetProcessRanking()
        {
            if (ProcessList.Count > 0)
            {
                foreach (var proc in ProcessList)
                {
                    var value = 0;

                    var item = _data.FirstOrDefault(p => p.EditedName == proc.EditedName);
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
        }
        public void DateChangeEvent()
        {
            if (pgOnDate != DateTime.Today)
            {
                pgOnDate = DateTime.Today;
                EndDate = DateTime.Today;
                //PresetChange();
            }
        }

        private async Task MonitorActiveWindow()
        {
            using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
            cts = new CancellationTokenSource();
            var token = cts.Token;

            try
            {
                while (await timer.WaitForNextTickAsync(token)) // 5초마다 실행
                {
                    _ = GetWindowProcess();

                    _ = SetProcessRanking();

                    DateChangeEvent();
                }
            }
            catch (OperationCanceledException)
            {

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
            StartDate = DateTime.Today.AddDays(-1);
            EndDate = DateTime.Today;
        }
        [RelayCommand]
        private void SetPeriodWeek()
        {
            StartDate = DateTime.Today.AddDays(-7);
            EndDate = DateTime.Today;
        }
        [RelayCommand]
        private void SetPeriodMonth()
        {
            StartDate = DateTime.Today.AddMonths(-1);
            EndDate = DateTime.Today;
        }
        [RelayCommand]
        private void PresetChange()
        {
            _xmlSetService.SaveSelectedPreset(SelectedPreset.ToString()!);

            List<ProcessUsage> processes = new();
            List<ProcessUsage> EntireProcesses = new();
            ProcessList.Clear();
            EntireProcessList.Clear();

            processes = _xmlSetService.LoadPresetProcess(SelectedPreset.ToString()!);
            EntireProcesses = _databaseService.QueryEntireProcessData();

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
            SecChecked = true;
            counter = 1;
            UpdateProcessList();
        }
        [RelayCommand]
        private void SetMinute()
        {
            MinChecked = true;
            counter = 12;
            UpdateProcessList();
        }
        [RelayCommand]
        private void SetHour()
        {
            HourChecked = true;
            counter = 720;
            UpdateProcessList();
        }
        [RelayCommand]
        private void BarClick(PointerCommandArgs args)
        {
            List<(int,string)> dayTimeData = new();
            List<DateTimePoint> dayTimePoint = new();
            SolidColorPaint chartColor = new SolidColorPaint { Color = SKColors.Yellow };
            bool isFound = false;

            var foundPoints = args.Chart.GetPointsAt(args.PointerPosition);

            foreach (var point in foundPoints)
            {
                var geometry = (DrawnGeometry)point.Context.Visual!;

                if (!_activePoints.Contains(point))
                {
                    //geometry.Fill = new SolidColorPaint { Color = SKColors.Yellow };
                    _activePoints.Add(point);
                }
                else
                {
                    // clear the fill to the default value
                    //geometry.Fill = null;
                    _activePoints.Remove(point);
                }

                Trace.WriteLine($"found {point.Context.DataSource}");

                if (point.Context.DataSource is ProcessChartInfo data)
                {
                    chartColor = data.Paint;
                    dayTimeData = _databaseService.QueryDayTimeData(data?.Name, StartDate.ToString(), EndDate.ToString());
                }

                isFound = true;
            }

            if (isFound)
            {
                foreach (var point in dayTimeData)
                {
                    if (DateTime.TryParse(point.Item2, out DateTime date))
                    {
                        DateTimePoint data = new() { DateTime = date, Value = point.Item1 / 12 };
                        dayTimePoint.Add(data);
                    }
                }
                var series = new ColumnSeries<DateTimePoint>
                {
                    Values = dayTimePoint,
                    DataLabelsPaint = new SolidColorPaint(new SKColor(245, 245, 245))
                    {
                        SKTypeface = SKTypeface.FromFamilyName("Arial", new SKFontStyle(SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)) // Correct usage
                    },
                    DataLabelsPosition = DataLabelsPosition.Middle,
                    DataLabelsTranslate = new(0, 0),
                    DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue}",
                    DataLabelsSize = 25,

                    MaxBarWidth = 100,
                    Rx = 15,
                    Ry = 15,
                    Padding = 5,
                }
                .OnPointMeasured(point =>
                {
                    if (point.Visual is null) return;
                    point.Visual.Fill = chartColor;
                });

                Series2 = [series];

                Thread.Sleep(100);
                TimeEnable = false;
                PresetEnable = false;
                IsChart1Visible = false;
                IsChart2Visible = true;
                IsGifVisible = false;
            }
        }

        [RelayCommand]
        private void Back()
        {
            TimeEnable = true;
            PresetEnable = true;
            IsChart1Visible = true;
            IsChart2Visible = false;
            IsGifVisible = true;
        }
    }
}

            