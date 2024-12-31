using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WindowsScreenTime.Services;
using WindowsScreenTime.ViewModels;

namespace WindowsScreenTime
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    /// 
    public partial class App : Application
    {
        public App()
        {
            Services = ConfigureServices();
            InitializeComponent();
        }
        public new static App Current => (App)Application.Current;

        public IServiceProvider Services { get; private set; }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddSingleton<MainViewModel>();
            services.AddSingleton<HomeViewModel>();
            services.AddSingleton<EditViewModel>();
            services.AddSingleton<SettingsViewModel>();

            services.AddTransient<IIniSetService, IniSetService>();

            return services.BuildServiceProvider();
        }

    }

}
