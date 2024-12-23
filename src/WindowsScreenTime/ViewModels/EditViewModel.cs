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
using System.Collections.Concurrent;
using Prism.Regions;

namespace WindowsScreenTime.ViewModels
{
    public partial class EditViewModel : ObservableObject, IDisposable
    {
        public ObservableCollection<ProcessUsage> ProcessList { get; set; }
        public ObservableCollection<FileItem> ViewList { get; set; }


        private readonly Dictionary<string, WeakReference<BitmapImage>> iconCache;

        private CancellationTokenSource? _cts;
        private bool _disposed = false;

        [ObservableProperty]
        private FileItem selectedProcess;
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
            totalRam = GetTotalRam();

            ProcessList = new();
            ViewList = new();
            iconCache = new();
            _cts = new();

            Task.Run(PeriodicProcessUpdate);

            //ProcessUsage item = new();
            //item.ProcessName = "asd";
            //item.RamUsagePer = 123;
            //item.UsageTime = 123;
            //item.ExecutablePath = "12";
            FileItem item = new();
            item.Name = "asd";
            item.Path = "asd";
            item.Size = null;
            item.Type = "Folder";
            item.Depth = 1;
            ViewList.Add(item);

        }

        public void Dispose()
        {
            if (ProcessList != null)
            {
                ProcessList.Clear();
                ProcessList = null;
            }
            GC.SuppressFinalize(this);
        }

        private async Task PeriodicProcessUpdate()
        {
            while (true)
            {
                if (_cts.IsCancellationRequested) return;
                UpdateProcesses();

                await Task.Delay(5000); // 5초 간격 업데이트
            }
        }
        private async void UpdateProcesses()
        {
            var processes = Process.GetProcesses();
            var updatedProcesses = await Task.Run(() =>
            {
                var processUsageMap = new Dictionary<string, ProcessUsage>();

                foreach (var process in processes)
                {
                    if (string.Equals(process.ProcessName, "Idle", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(process.ProcessName, "System", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(process.ProcessName, "WindowsScreenTime", StringComparison.OrdinalIgnoreCase))

                        continue;

                    try
                    {
                        var path = GetProcessPath(process);
                        if (path == null) continue;
                  
                        BitmapImage? icon = GetCachedIcon(path);

                        if (!processUsageMap.TryGetValue(process.ProcessName, out var existingUsage))
                        {
                            processUsageMap[process.ProcessName] = new ProcessUsage
                            {
                                ProcessIcon = icon,
                                ProcessName = process.ProcessName,
                                MemorySize = process.WorkingSet64,
                                RamUsagePer = (double)process.WorkingSet64 / totalRam * 100,
                                ExecutablePath = path
                            };
                        }
                        else
                        {
                            existingUsage.MemorySize += process.WorkingSet64;
                            existingUsage.RamUsagePer += (double)process.WorkingSet64 / totalRam * 100;
                        }
                    }
                    catch
                    {
                        // 접근 불가한 프로세스 무시
                    }
                }
                return processUsageMap.Values
                                    .Where(p => MatchesFilter(p, FilterText))
                                    .OrderByDescending(p => p.RamUsagePer)
                                    .ToList();
            });

            // UI 업데이트
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 제거해야 할 항목 찾기
                for (int i = ProcessList.Count - 1; i >= 0; i--)
                {
                    var existing = ProcessList[i];
                    if (!updatedProcesses.Any(p => p.ProcessName == existing.ProcessName))
                    {
                        ProcessList.RemoveAt(i);
                    }
                }
                // 새로운 항목 추가 및 업데이트
                foreach (var process in updatedProcesses)
                {
                    var existingIndex = ProcessList.ToList().FindIndex(p => p.ProcessName == process.ProcessName);
                    if (existingIndex == -1)
                    {
                        ProcessList.Add(process);
                    }
                    else
                    {
                        ProcessList[existingIndex] = process;
                    }
                }
                GC.Collect();
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

        private BitmapImage? GetCachedIcon(string path)
        {
            if (iconCache.TryGetValue(path, out var weakRef))
            {
                if (weakRef.TryGetTarget(out var cachedIcon))
                {
                    return cachedIcon;
                }
                iconCache.Remove(path);
            }

            var icon = GetProcessIcon(path);
            if (icon != null)
            {
                CleanupIconCache();
                iconCache[path] = new WeakReference<BitmapImage>(icon);
                icon.Freeze(); // 중요: UI 스레드 간 공유 가능하게 만듦
            }
            return icon;
        }

        private void CleanupIconCache()
        {
            var keysToRemove = iconCache.Keys
                .Where(key => !iconCache[key].TryGetTarget(out _))
                .ToList();

            foreach (var key in keysToRemove)
            {
                iconCache.Remove(key);
            }
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
                        bitmap.DecodePixelWidth = 32;
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
            ViewList.Add(SelectedProcess);
        }

        [RelayCommand]
        private void RemoveViewList()
        {
            ViewList.Remove(SelectedProcess);
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
