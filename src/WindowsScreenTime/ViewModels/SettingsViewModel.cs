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
using Microsoft.Win32.TaskScheduler;
using System.Reflection;
using System.IO;


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
        private const string AppName = "WindowsScreenTime";
        string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

        public SettingsViewModel()
        {
            // 시작프로그램 등록 여부 확인
            using (var regKey = Registry.CurrentUser.OpenSubKey(_startupRegPath, false))
            {
                if (regKey.GetValue("WindowsScreenTime") == null)
                    IsAutoStart = false;
                else
                    IsAutoStart = true;
            }

            if (IsNightMode)
                ThemeManager.ChangeTheme(ThemeType.Dark);
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

            //using (TaskService ts = new TaskService())
            //{
            //    // 기존 작업 제거
            //    var existing = ts.GetTask(TaskName);
            //    if (existing != null)
            //        ts.RootFolder.DeleteTask(TaskName);

            //    TaskDefinition td = ts.NewTask();
            //    td.RegistrationInfo.Description = "자동 시작용 작업";

            //    // 로그인 시 실행
            //    td.Triggers.Add(new LogonTrigger());

            //    // 프로그램 실행
            //    td.Actions.Add(new ExecAction(exePath, null, Path.GetDirectoryName(exePath)));

            //    // 관리자 권한으로 실행
            //    //td.Principal.RunLevel = TaskRunLevel.Highest;

            //    // 등록
            //    ts.RootFolder.RegisterTaskDefinition(TaskName, td);
            //}
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
            //using (TaskService ts = new TaskService())
            //{
            //    var existing = ts.GetTask(TaskName);
            //    if (existing != null)
            //        ts.RootFolder.DeleteTask(TaskName);
            //}
        }

        public static bool IsRegistered()
        {
            using (TaskService ts = new TaskService())
            {
                return ts.GetTask(AppName) != null;
            }
        }

        [RelayCommand]
        public void AutoStartChange()
        {
            if (IsAutoStart)
            {
                AddStartupProgram();
                IsAutoStart = true;
            }
            else
            {
                RemoveStartupProgram();
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
