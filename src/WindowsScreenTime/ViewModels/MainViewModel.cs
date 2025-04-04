using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Threading;
using WindowsScreenTime.Models;

namespace WindowsScreenTime.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private object? _currentView;
        public object CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public ICommand? _menuCommand;
        public ICommand MenuCommand
        {
            get
            {
                return _menuCommand ??
                    (_menuCommand = new RelayCommand(GoMenu));      // 1. viewmodel 변경시 커맨드 실행
            }
        }

        public ICommand? _homeCommand;
        public ICommand HomeCommand
        {
            get
            {
                return _homeCommand ??
                    (_homeCommand = new RelayCommand(GoHome));      // 1. viewmodel 변경시 커맨드 실행
            }
        }

        public ICommand? _editCommand;
        public ICommand EditCommand
        {
            get
            {
                return _editCommand ??
                    (_editCommand = new RelayCommand(GoEdit));      // 1. viewmodel 변경시 커맨드 실행
            }
        }

        public ICommand? _settingsCommand;
        public ICommand SettingsCommand
        {
            get
            {
                return _settingsCommand ??
                    (_settingsCommand = new RelayCommand(GoSettings));      // 1. viewmodel 변경시 커맨드 실행
            }
        }


        public MainViewModel()
        {
            string path = "C:\\RUO_data\\startup_log.txt";

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                File.AppendAllText(path, $"Unhandled Exception: {e.ExceptionObject}\n");
            };

            try
            {
                File.AppendAllText(path, $"{DateTime.Now}: 프로그램 실행됨!\n");

                // 원래 실행할 코드
            }
            catch (Exception ex)
            {
                File.AppendAllText(path, $"{DateTime.Now}: 오류 발생 - {ex}\n");
            }

            _ = Initialize();
        }

        private async Task Initialize()
        {   
            await Task.Delay(5000); // Simulate some initialization work
            GoHome();
            //EnsureRunAsAdmin();
        }

        private void GoMenu()
        {
            
        }

        private void GoHome()
        {
            CurrentView = App.Current.Services.GetService(typeof(HomeViewModel));
            var message = new TransferViewModelActivation { isActivated = false };
            WeakReferenceMessenger.Default.Send(message);
        }
        private void GoEdit()
        {
            CurrentView = App.Current.Services.GetService(typeof(EditViewModel));
            var message = new TransferViewModelActivation { isActivated = true };
            WeakReferenceMessenger.Default.Send(message);
        }
        private void GoSettings()
        {
            CurrentView = App.Current.Services.GetService(typeof(SettingsViewModel));
            var message = new TransferViewModelActivation { isActivated = false };
            WeakReferenceMessenger.Default.Send(message);
        }

        public static void EnsureRunAsAdmin()
        {
            // 현재 프로세스가 관리자 권한으로 실행 중인지 확인
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

                if (!isAdmin)
                {
                    // 관리자 권한으로 다시 실행
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = Process.GetCurrentProcess().MainModule.FileName,
                        Verb = "runas", // 관리자 권한 요청
                        UseShellExecute = true
                    };

                    try
                    {
                        Process.Start(startInfo);
                        Environment.Exit(0); // 현재 프로세스 종료
                    }
                    catch
                    {
                        Console.WriteLine("❌ 관리자 권한 요청 실패!");
                    }
                }
            }
        }


    }

}
