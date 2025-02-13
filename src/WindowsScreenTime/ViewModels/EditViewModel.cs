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
using System.DirectoryServices.ActiveDirectory;

namespace WindowsScreenTime.ViewModels
{
    public partial class EditViewModel : ObservableObject
    {
        private readonly IIniSetService _iniSetService;
        private readonly IXmlSetService _xmlSetService;
        private readonly IProcessContainService _processContainService;
        
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
        private int selectedPreset;
                    
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

        private List<string?> ViewListStr;
        private readonly double totalRam;

        private Task _processSerchTask;

        private static string ResourcePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources/Icons");

        public EditViewModel(IXmlSetService xmlSetService, IProcessContainService processContainService)
        {
            _xmlSetService = xmlSetService;
            _processContainService = processContainService;

            WeakReferenceMessenger.Default.Register<TransferViewModelActivation>(this, OnTransferViewModelState);

            totalRam = GetTotalRam();

            ProcessList = new();
            ViewList = new();
            iconCache = new();
            ViewListStr = new();

            if (!Directory.Exists(ResourcePath))
                Directory.CreateDirectory(ResourcePath);
        }

        public void Initialize()
        {
            _cts = new();
            var token = _cts.Token;
            _processSerchTask = Task.Run(() => PeriodicProcessUpdate(token));

            if (_xmlSetService.LoadSelectedPreset() == null || _xmlSetService.LoadSelectedPreset() == "")
                SelectedPreset = 0;
            else
            {
                SelectedPreset = Convert.ToInt32(_xmlSetService.LoadSelectedPreset());
                PresetChange();
            }
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
                        var path = _processContainService.GetProcessPath(process);
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

            var icon = _processContainService.GetProcessIcon(path);
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
                _xmlSetService.SaveSelectedPreset(SelectedPreset.ToString());

                List<ProcessUsage> processes = new();
                ViewList.Clear();
                processes = _xmlSetService.LoadPresetProcess(SelectedPreset.ToString());

                if (processes != null)
                {
                    foreach (ProcessUsage proc in processes)
                    {
                        string? processName = proc.ProcessName;

                        if (processName != null)
                        {
                            string? path = _processContainService.GetProcessPathByString(processName); 
                            // 프로세스 이름을 바탕으로 프로세스 경로를 찾는다
                            if (path != null)
                            {
                                BitmapImage? icon = GetCachedIcon(path);
                                proc.ProcessIcon = icon;
                            }
                        }
                        ViewList.Add(proc);
                    }
                }
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

        private void SaveBitmapImage(BitmapImage bitmapImage, string filePath)
        {
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapImage));

            if (File.Exists(filePath))
                return;
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                encoder.Save(fileStream);
            }
        }

        [RelayCommand]
        private void PresetSave()
        {
            foreach (ProcessUsage process in ViewList)
            {
                if (process.EditedName == null)
                    process.EditedName = process.ProcessName;

                string iconPath = Path.Combine(ResourcePath, process.ProcessName + ".png");

                if (process.ProcessIcon != null)
                {
                    SaveBitmapImage(process.ProcessIcon, iconPath);
                }
            }
            _xmlSetService.SavePreset(SelectedPreset.ToString(), ViewList);
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
