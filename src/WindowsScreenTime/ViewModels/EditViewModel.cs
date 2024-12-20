using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using WindowsScreenTime.Models;
using System.Windows;
using System.Management;
using Cupertino.Support.Local.Helpers;
using Cupertino.Support.Local.Models;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Drawing;

namespace WindowsScreenTime.ViewModels
{
    public partial class EditViewModel : ObservableObject
    {
        public ObservableCollection<ProcessUsage> ProcessList { get; set; }
        public ObservableCollection<ProcessUsage> ViewList { get; set; }

        [ObservableProperty]
        private ProcessUsage selectedProcess;
        [ObservableProperty]
        public ListBoxItem selectedPreset;
        

        private string filterText;
        public string FilterText
        {
            get => filterText;
            set
            {
                filterText = value;
                UpdateProcesses();
            }
        }

        private readonly double totalRam;        

        public EditViewModel()
        {
            ProcessList = new ObservableCollection<ProcessUsage>();
            ViewList = new ObservableCollection<ProcessUsage>();
            totalRam = GetTotalRam();

            Task.Run(PeriodicProcessUpdate);
        }

        private async Task PeriodicProcessUpdate()
        {
            while (true)
            {
                UpdateProcesses();
                await Task.Delay(3000); // 3초 간격 업데이트
            }
        }
        private void UpdateProcesses()
        {
            var processes = Process.GetProcesses();
       
            var updatedProcesses = processes
                .Where(p => !string.Equals(p.ProcessName, "Idle", StringComparison.OrdinalIgnoreCase)
                            && !string.Equals(p.ProcessName, "System", StringComparison.OrdinalIgnoreCase))
                .Select(p =>
                {
                    try
                    {
                        var path = GetProcessPath(p);  // 프로세스 경로 확인
                        if (path == null) return null; // 경로를 얻을 수 없으면 제외

                        return new ProcessUsage
                        {
                            ProcessIcon = GetProcessIcon(path),
                            ProcessName = p.ProcessName,
                            MemorySize = p.WorkingSet64,
                            RamUsagePer = (p.WorkingSet64 / totalRam) * 100,
                            ExecutablePath = GetProcessPath(p)
                        };
                    }
                    catch
                    {
                        return null; // 접근 불가한 프로세스는 무시
                    }
                })
                .Where(p => p != null && MatchesFilter(p, FilterText))
                .OrderByDescending(p => p.RamUsagePer)
                .ToList();

            App.Current.Dispatcher.Invoke(() =>
            {
                ProcessList.Clear();
                foreach (var process in updatedProcesses)
                {
                    ProcessList.Add(process);
                }
            });                 
        }

        private bool MatchesFilter(ProcessUsage process, string filterWord)
        {
            if (string.IsNullOrWhiteSpace(filterWord))
                return true; // 필터가 비어 있으면 모두 포함


            return
                (!string.IsNullOrEmpty(process.ProcessName) && process.ProcessName.Contains(filterWord, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(process.ExecutablePath) && process.ExecutablePath.Contains(filterWord, StringComparison.OrdinalIgnoreCase));
        }

        private BitmapImage GetProcessIcon(string path)
        {
            try
            {
                Icon? icon = Icon.ExtractAssociatedIcon(path);
                if (icon == null) return null;

                using (var stream = new MemoryStream())
                {
                    icon.ToBitmap().Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    stream.Seek(0, SeekOrigin.Begin);

                    BitmapImage bitmap = null;
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = stream;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze(); // 스레드 안전성을 위해 Freeze 호출
                    });

                    return bitmap;
                }
            }
            catch
            {
                return null; // 아이콘을 가져올 수 없으면 null 반환
            }
        }

        private string GetProcessPath(Process process)
        {
            try
            {
                return process.MainModule.FileName;
            }
            catch
            {
                return null; // 경로에 접근할 수 없을 경우
            }
        }

        private double GetTotalRam()
        {
            double totalMemory = 0;

            using (var searcher = new ManagementObjectSearcher("SELECT Capacity FROM Win32_PhysicalMemory"))
            {
                foreach (var obj in searcher.Get())
                {
                    totalMemory += Convert.ToDouble(obj["Capacity"]);
                }
            }

            return totalMemory;
        }

        [RelayCommand]
        private void AddViewList()
        {
            ViewList.Add(selectedProcess);
        }

        [RelayCommand]
        private void RemoveViewList()
        {
            ViewList.Remove(selectedProcess);
        }

        [RelayCommand]
        private void PresetChange()
        {
            if (SelectedPreset != null)
            {
                int presetIndex = (int)SelectedPreset.Content;
            }
        }
    }
}
