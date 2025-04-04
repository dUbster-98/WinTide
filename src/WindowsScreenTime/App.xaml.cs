using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WindowsScreenTime.Services;
using WindowsScreenTime.ViewModels;
using WindowsScreenTime.Views;

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
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            // 시작 프로그램으로 실행시, 작업 디렉토리가 앱 실행경로가 아니기 때문에 발생하는 오류 방지
            InitializeComponent();
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
            }));
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
            services.AddTransient<AlarmPopupViewModel>();

            services.AddTransient<IIniSetService, IniSetService>();
            services.AddTransient<IXmlSetService, XmlSetService>();
            services.AddTransient<IProcessContainService, ProcessContainService>();
            services.AddTransient<IDatabaseService, DatabaseService>();

            return services.BuildServiceProvider();
        }

    }

}
