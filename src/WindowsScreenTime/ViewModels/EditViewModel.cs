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
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Collections.Concurrent;
using Prism.Regions;
using WindowsScreenTime.Services;
using CommunityToolkit.Mvvm.Messaging;

namespace WindowsScreenTime.ViewModels
{
    public partial class EditViewModel : ObservableObject
    {
        private readonly IIniSetService _iniSetService;
        public ObservableCollection<ProcessUsage> ProcessList { get; set; }
        public ObservableCollection<ProcessUsage> ViewList { get; set; }

        private readonly Dictionary<string, WeakReference<BitmapImage>> iconCache;

        public CancellationTokenSource? _cts;

        [ObservableProperty]
        private ProcessUsage? selectedProcess;
        private ProcessUsage selectedProcessTmp;
        [ObservableProperty]
        private ProcessUsage? itemToRemove;
        [ObservableProperty]
        private ListBoxItem selectedPreset;

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
        private int presetIndex { get; set; }

        private List<string?> ViewListStr;
        private readonly double totalRam;

        private Task _processSerchTask;

        public EditViewModel(IIniSetService iniSetService)
        {
            _iniSetService = iniSetService;

            WeakReferenceMessenger.Default.Register<TransferViewModelActivation>(this, OnTransferViewModelState);

            totalRam = GetTotalRam();

            ProcessList = new();
            ViewList = new();
            iconCache = new();
            ViewListStr = new();

        }

        public void Initialize()
        {
            _cts = new();
            var token = _cts.Token;
            _processSerchTask = Task.Run(() => PeriodicProcessUpdate(token));
        }

        public void StopTask()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }

            _processSerchTask = null;
        }

        private async Task PeriodicProcessUpdate(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                UpdateProcesses();
                SelectedProcess = selectedProcessTmp;
                await Task.Delay(5000, token); // 5초 간격 업데이트
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
            if (SelectedProcess == null)
                return;

            if (ViewListStr.Contains(SelectedProcess.ProcessName))
                return;

            ViewListStr.Add(SelectedProcess.ProcessName);
            ViewList.Add(SelectedProcess);                
        }

        [RelayCommand]
        private void RemoveViewList()
        {
            if (ItemToRemove == null)
                return;

            ViewListStr.Remove(ItemToRemove.ProcessName);
            ViewList.Remove(ItemToRemove);
        }

        [RelayCommand]
        private void PresetChange()
        {
            if (SelectedPreset != null)
            {
                presetIndex = Convert.ToInt32(SelectedPreset.Content);
            }
        }

        [RelayCommand]
        private void ProcessSelect()
        {
            if (SelectedProcess != null)
            {
                selectedProcessTmp = SelectedProcess;
            }
        }

        [RelayCommand]
        private void PresetSave()
        {
            string[] ViewProcess = ViewList.Select(ProcessUsage => ProcessUsage.ProcessName).ToArray();

            foreach (string process in ViewProcess)
            {
                _iniSetService.SetIni(SelectedPreset.ToString(), process, "", IniSetService.path);
            }

            foreach (ProcessUsage process in ViewList)
            {
                _iniSetService.SetIni(SelectedPreset.ToString(), process.ProcessName, process.EditedName, IniSetService.path);
            }
        }

        private void OnTransferViewModelState(object recipient, TransferViewModelActivation message)
        {
            if (message.isActivated == true)
            {
                Initialize();
            }
            else
            {
                StopTask();
            }
        }
    }
}
