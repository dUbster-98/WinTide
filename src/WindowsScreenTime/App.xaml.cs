using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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

            services.AddSingleton(typeof(MainViewModel));
            services.AddSingleton(typeof(HomeViewModel));
            services.AddSingleton(typeof(EditViewModel));
            services.AddSingleton(typeof(SettingsViewModel));

            return services.BuildServiceProvider();
        }
    }

}
