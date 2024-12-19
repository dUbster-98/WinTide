using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

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
            CurrentView = new HomeViewModel();
        }

        private void GoMenu()
        {
            
        }
        private void GoHome()
        {
            CurrentView = new HomeViewModel();
        }
        private void GoEdit()
        {
            CurrentView = new EditViewModel();
        }
        private void GoSettings()
        {
            CurrentView = new SettingsViewModel();
        }


    }

}
