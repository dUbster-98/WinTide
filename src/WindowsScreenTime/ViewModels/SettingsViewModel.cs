using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WindowsScreenTime.Themes;
using System.Configuration;
using System.Diagnostics;
using System.Windows.Input;
using System.Reflection;
using System.IO;
using WindowsScreenTime.Services;
using CommunityToolkit.Mvvm.Messaging;
using WindowsScreenTime.Models;


namespace WindowsScreenTime.ViewModels
{
    public partial class SettingsViewModel :ObservableObject
    {
        private readonly IXmlSetService _xmlSetService;

        [ObservableProperty]
        private bool isAutoStart;
        [ObservableProperty]
        private bool isAdmin;
        [ObservableProperty]
        private bool isBackground;
        [ObservableProperty]
        private bool isNightMode;
        [ObservableProperty]
        private bool isGifShow;

        // 부팅시 시작 프로그램을 등록하는 레지스트리 경로
        private static readonly string _startupRegPath = 
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "WindowsScreenTime";
        string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

        public SettingsViewModel(IXmlSetService xmlSetService)
        {
            _xmlSetService = xmlSetService;

            // 시작프로그램 등록 여부 확인
            using (var regKey = Registry.CurrentUser.OpenSubKey(_startupRegPath, false))
            {
                if (regKey.GetValue("WindowsScreenTime") == null)
                    IsAutoStart = false;
                else
                    IsAutoStart = true;
            }

            if(_xmlSetService.LoadConfig("Admin") == true)
                IsAdmin = true;
            else IsAdmin = false;

            if (_xmlSetService.LoadConfig("Background") == true)
                IsBackground = true;
            else IsBackground = false;

            if (_xmlSetService.LoadConfig("GifShow") == true)
            {
                IsGifShow = true;
            }
            else IsGifShow = false;

            if (_xmlSetService.LoadConfig("DarkTheme") == true)
            {
                IsNightMode = true;
                ThemeManager.ChangeTheme(ThemeType.Dark);
            }
            else IsNightMode = false;
        }

        // 부팅시 시작 프로그램 등록
        public void AddStartupProgram()
        {
            using (RegistryKey regKey = Registry.CurrentUser.OpenSubKey(_startupRegPath, true))
            {
                try
                {
                    // 키가 이미 등록돼 있지 않을때만 등록
                    if (regKey.GetValue(AppName) == null)
                        regKey.SetValue(AppName, exePath);

                    regKey.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        // 등록된 프로그램 제거
        public void RemoveStartupProgram()
        {
            using (var regKey = Registry.CurrentUser.OpenSubKey(_startupRegPath, true))
            {
                try
                {
                    // 키가 이미 존재할때만 제거
                    if (regKey.GetValue(AppName) != null)
                        regKey.DeleteValue(AppName, false);

                    regKey.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        [RelayCommand]
        public void AutoStartChange()
        {
            if (IsAutoStart)
            {
                AddStartupProgram();          
            }
            else
            {
                RemoveStartupProgram();
            }
        }

        [RelayCommand]
        public void AdminChange()
        {
            if (IsAdmin)
            {
                _xmlSetService.SaveConfig("Admin", true);
            }
            else
            {
                _xmlSetService.SaveConfig("Admin", false);
            }
        }

        [RelayCommand]
        public void BackgroundChange()
        {
            if (IsBackground)
            {
                _xmlSetService.SaveConfig("Background", true);
            }
            else
            {
                _xmlSetService.SaveConfig("Background", false);
            }
        }

        [RelayCommand]
        public void GifShowChange()
        {
            if (IsGifShow)
            {
                _xmlSetService.SaveConfig("GifShow", true);
                var message = new TransferIsGifShowChange { isVisible = true };
                WeakReferenceMessenger.Default.Send(message);
            }
            else
            {
                _xmlSetService.SaveConfig("DarkTheme", false);
                var message = new TransferIsGifShowChange { isVisible = false };
                WeakReferenceMessenger.Default.Send(message);
            }
        }

        [RelayCommand]
        public void ThemeChange()
        {
            if (IsNightMode)
            {
                _xmlSetService.SaveConfig("DarkTheme", true);
                ThemeManager.ChangeTheme(ThemeType.Dark);
            }
            else
            {
                ThemeManager.ChangeTheme(ThemeType.Light);
                _xmlSetService.SaveConfig("DarkTheme", false);
            }
        }
    }
}
