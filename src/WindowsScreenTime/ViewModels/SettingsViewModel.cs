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


namespace WindowsScreenTime.ViewModels
{
    public partial class SettingsViewModel :ObservableObject
    {
        [ObservableProperty]
        private bool isAutoStart;
        [ObservableProperty]
        private bool isNightMode;

        // 부팅시 시작 프로그램을 등록하는 레지스트리 경로
        private static readonly string _startupRegPath = 
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        public SettingsViewModel()
        {
            // 시작프로그램 등록 여부 확인
            using (var regKey = GetRegKey(_startupRegPath, false))
            {
                if (regKey.GetValue("WindowsScreenTime") == null)
                    IsAutoStart = false;
                else
                    IsAutoStart = true;
            }
            if (IsNightMode)
                ThemeManager.ChangeTheme(ThemeType.Dark);
        }

        private Microsoft.Win32.RegistryKey GetRegKey(string regPath, bool writable)
        {
            return Microsoft.Win32.Registry.CurrentUser.OpenSubKey(regPath, writable);
        }

        // 부팅시 시작 프로그램 등록
        public void AddStartupProgram(string programName, string executablePath)
        {
            using (var regKey = GetRegKey(_startupRegPath, true))
            {
                try
                {
                    // 키가 이미 등록돼 있지 않을때만 등록
                    if (regKey.GetValue(programName) == null)
                        regKey.SetValue(programName, executablePath);

                    regKey.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        // 등록된 프로그램 제거
        public void RemoveStartupProgram(string programName)
        {
            using (var regKey = GetRegKey(_startupRegPath, true))
            {
                try
                {
                    // 키가 이미 존재할때만 제거
                    if (regKey.GetValue(programName) != null)
                        regKey.DeleteValue(programName, false);

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
                AddStartupProgram("WindowsScreenTime", System.Reflection.Assembly.GetExecutingAssembly().Location);
                IsAutoStart = true;
            }
            else
            {
                RemoveStartupProgram("WindowsScreenTime");
                IsAutoStart = false;
            }
        }

        [RelayCommand]
        public void ThemeChange()
        {
            if (IsNightMode)
                ThemeManager.ChangeTheme(ThemeType.Dark);
            else
                ThemeManager.ChangeTheme(ThemeType.Light);
            //ThemeManager.ToggleTheme();
        }
    }
}
