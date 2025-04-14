using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsScreenTime.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WindowsScreenTime.Services
{
    public interface INavigationService
    {
        void NavigateTo<T>() where T : ObservableObject;
        void NavigateTo<T>(object parameter) where T : ObservableObject;
    }

    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _services;
        private readonly Dictionary<Type, ObservableObject> _vmCache = new();

        public NavigationService(IServiceProvider services, MainViewModel mainViewModel)
        {
            _services = services;
        }

        public void NavigateTo<T>() where T : ObservableObject
        {
            var mainViewModel = _services.GetRequiredService<MainViewModel>();

            if (!_vmCache.TryGetValue(typeof(T), out var vm))
            {
                vm = (ObservableObject)_services.GetRequiredService(typeof(T));
                _vmCache[typeof(T)] = vm;
            }

            mainViewModel.CurrentView = vm;
        }

        public void NavigateTo<T>(object parameter) where T : ObservableObject
        {
            // 필요한 경우 ViewModel에서 파라미터 처리
            //var vm = (ObservableObject)_services.GetRequiredService(typeof(T));

            //if (vm is IInitializeWithParameter init)
            //{
            //    init.Initialize(parameter);
            //}

            //_mainViewModel.CurrentView = vm;
        }
    }


}
