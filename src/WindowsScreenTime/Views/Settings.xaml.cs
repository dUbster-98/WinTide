using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WindowsScreenTime.ViewModels;

using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace WindowsScreenTime.Views
{
    /// <summary>
    /// Settings.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Settings : UserControl
    {
        public Settings()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(SettingsViewModel));
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            WindowColor.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#393939"));
            WindowColor.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0FFFF"));
        }
        private void ToggleButton_UnChecked(object sender, RoutedEventArgs e)
        {
            WindowColor.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"));
            WindowColor.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#191919"));
        }

    }
}
