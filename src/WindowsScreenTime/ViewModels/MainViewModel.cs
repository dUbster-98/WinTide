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
using System.Windows.Shapes;
using WindowsScreenTime.Services;

namespace WindowsScreenTime.ViewModels
{
    public class WindowActionEventArgs : EventArgs
    {
        public string Action { get; }

        public WindowActionEventArgs(string action)
        {
            Action = action;
        }
    }

    public partial class MainViewModel : ObservableObject
    {
        private readonly IServiceProvider _services;
        private readonly IXmlSetService _xmlSetService;

        public event EventHandler<WindowActionEventArgs>? RequestWindowAction;

        [ObservableProperty]
        private bool isGifVisible = false;

        private readonly Dictionary<Type, ObservableObject> _vmCache = new();

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

        public MainViewModel(IServiceProvider services, IXmlSetService xmlSetService)
        {
            //string path = "C:\\Test\\startup_log.txt";

            //AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            //{
            //    File.AppendAllText(path, $"Unhandled Exception: {e.ExceptionObject}\n");
            //};
            //try
            //{
            //    File.AppendAllText(path, $"{DateTime.Now}: 프로그램 실행됨!\n");

            //    // 원래 실행할 코드
            //}
            //catch (Exception ex)
            //{
            //    File.AppendAllText(path, $"{DateTime.Now}: 오류 발생 - {ex}\n");
            //}
            _services = services;
            _xmlSetService = xmlSetService;

            WeakReferenceMessenger.Default.Register<TransferIsGifShowChange>(this, OnTransferIsGifShowChange);

            if (_xmlSetService.LoadConfig("GifShow") == true)
                IsGifVisible = true;

            Initialize();
        }

        public MainViewModel()
        {

        }

        private void Initialize()
        {
            if (_xmlSetService.LoadConfig("Admin") == true)
                EnsureRunAsAdmin();

            GoHome();
        }

        public void SetWindow()
        {

        }

        private void GoMenu()
        {
            
        }
        private void OnTransferIsGifShowChange(object recipient, TransferIsGifShowChange message)
        {
            if (message.isVisible == true)
                IsGifVisible = true;
            else
                IsGifVisible = false;
        }

        private void GoHome()
        {
            //CurrentView = App.Current.Services.GetService(typeof(HomeViewModel));
            NavigateTo<HomeViewModel>();
            var message = new TransferViewModelActivation { isActivated = false };
            WeakReferenceMessenger.Default.Send(message);
        }
        private void GoEdit()
        {
            //CurrentView = App.Current.Services.GetService(typeof(EditViewModel));
            NavigateTo<EditViewModel>();
            var message = new TransferViewModelActivation { isActivated = true };
            WeakReferenceMessenger.Default.Send(message);
        }
        private void GoSettings()
        {
            //CurrentView = App.Current.Services.GetService(typeof(SettingsViewModel));
            NavigateTo<SettingsViewModel>();
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

        [RelayCommand]
        public void WindowClosing()
        {
            bool isBackground = _xmlSetService.LoadConfig("Background");
            if (isBackground)
            {
                RequestWindowAction?.Invoke(this, new WindowActionEventArgs("Hide"));
            }
            else
            {
                RequestWindowAction?.Invoke(this, new WindowActionEventArgs("Close"));
            }
        }

        public void NavigateTo<T>() where T : ObservableObject
        {
            if (!_vmCache.TryGetValue(typeof(T), out var vm))
            {
                vm = (ObservableObject)_services.GetRequiredService(typeof(T));
                _vmCache[typeof(T)] = vm;
            }

            CurrentView = vm;
        }
    }

}
