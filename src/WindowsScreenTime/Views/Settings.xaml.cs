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
using System.Windows.Media.Animation;
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
        private void SetForegroundColorDay(DependencyObject parent)
        {
            foreach (var child in LogicalTreeHelper.GetChildren(parent))
            {
                if (child is Label label)
                {
                    label.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#393939"));  // 색상 변경
                }
                else if (child is DependencyObject dependencyObject)
                {
                    SetForegroundColorDay(dependencyObject);  // 재귀 호출
                }
            }
        }
        private void SetForegroundColorNight(DependencyObject parent)
        {
            foreach (var child in LogicalTreeHelper.GetChildren(parent))
            {
                if (child is Label label)
                {
                    label.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e3edf7"));  // 색상 변경
                }
                else if (child is DependencyObject dependencyObject)
                {
                    SetForegroundColorNight(dependencyObject);  // 재귀 호출
                }
            }
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            var backGroundAnimation = new ColorAnimation
            {
                To = (Color)ColorConverter.ConvertFromString("#393939"),
                Duration = new Duration(TimeSpan.FromSeconds(1))
            };

            var brush = Part_grid.Background as SolidColorBrush;
            brush.BeginAnimation(SolidColorBrush.ColorProperty, backGroundAnimation);


            SetForegroundColorNight(Part_grid);
        }

        private void ToggleButton_UnChecked(object sender, RoutedEventArgs e)
        {
            var backGroundAnimation = new ColorAnimation
            {
                To = (Color)ColorConverter.ConvertFromString("#e3edf7"),
                Duration = new Duration(TimeSpan.FromSeconds(1))
            };

            var brush = Part_grid.Background as SolidColorBrush;
            brush.BeginAnimation(SolidColorBrush.ColorProperty, backGroundAnimation);

            SetForegroundColorDay(Part_grid);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var opacityAnimation = new DoubleAnimation
            {
                To = 0.5,
                Duration = new Duration(TimeSpan.FromSeconds(1))
            };

            foreach (var child in Part_grid.Children)
            {
                if (child is Label label)
                {
                    label.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);
                }
            }
        }
    }
}
