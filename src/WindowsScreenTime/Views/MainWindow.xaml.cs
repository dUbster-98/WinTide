﻿using SmartDateControl.UI.Units;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading;
using WindowsScreenTime.ViewModels;
using System.IO;
using WpfAnimatedGif;

namespace WindowsScreenTime
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private static BitmapImage _image;
        private WeakReference<BitmapImage> _gifImageRef;

        public MainWindow()
        {
            InitializeComponent();
            var viewModel = (MainViewModel)App.Current.Services.GetService(typeof(MainViewModel));
            DataContext = viewModel;

            MaxHeight = SystemParameters.WorkArea.Height;
            PART_homeButton.IsChecked = true;
            PART_menuButton.IsChecked = true;

            viewModel.RequestWindowAction += (sender, args) =>
            {
                if (args.Action == "Hide")
                {
                    this.Hide();
                }
                else if (args.Action == "Close")
                {
                    this.Close();
                }
            };

            _image = new BitmapImage();
            _image.BeginInit();
            _image.UriSource = new Uri($"{Directory.GetCurrentDirectory()}/Resources/pic.gif");
            _image.DecodePixelWidth = 300; // 실제 표시되는 크기로 제한
            _image.DecodePixelHeight = 300;
            _image.CacheOption = BitmapCacheOption.None; // 메모리에 완전히 로드
            _image.EndInit();
            _image.Freeze();

            _gifImageRef = new WeakReference<BitmapImage>(_image);

            if (_gifImageRef != null && _gifImageRef.TryGetTarget(out var image))
            {
                ImageBehavior.SetAnimatedSource(MyGif, image);
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            //this.Close();
            this.Hide();       // 창 숨김
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void MinimizseButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.ActualWidth < 700)
            {
                VisualStateManager.GoToElementState(this, "SmallWindow", true);
                PART_menuButton.IsChecked = false;
            }
            else
            {
                VisualStateManager.GoToElementState(this, "LargeWindow", true);
                PART_menuButton.IsChecked = true;
            }

            if (PART_settingsButton.IsChecked == true)
            {
                var buttonY = PART_settingsButton.TranslatePoint(new System.Windows.Point(0, 0), this).Y;

                Storyboard storyboard = (Storyboard)FindResource("GoSettings_sb");

                foreach (var timeline in storyboard.Children)
                {
                    if (timeline is DoubleAnimation animation)
                    {
                        animation.To = buttonY - 100;
                        animation.Duration = new Duration(TimeSpan.FromMilliseconds(10));
                    }
                }
                storyboard.Begin(this);
            }
        }

        private void PART_settingsButton_Click(object sender, RoutedEventArgs e)
        {
            var buttonY = PART_settingsButton.TranslatePoint(new System.Windows.Point(0, 0), this).Y;
            // Storyboard 가져오기
            Storyboard storyboard = (Storyboard)FindResource("GoSettings_sb");

            // Storyboard에서 DoubleAnimation 가져오기
            foreach (var timeline in storyboard.Children)
            {
                if (timeline is DoubleAnimation animation)
                {
                    // 동적으로 To 값 설정
                    animation.To = buttonY - 100;
                }
            }

            // 애니메이션 실행
            storyboard.Begin(this);
        }

        private void PART_menuButton_Click(object sender, RoutedEventArgs e)
        {
            if (PART_menuButton.IsChecked == true)
            {
                Storyboard storyboard = (Storyboard)FindResource("OpenMenu_sb");
                storyboard.Begin(this);
            }
            else
            {
                Storyboard storyboard = (Storyboard)FindResource("CloseMenu_sb");
                storyboard.Begin(this);
            }
        }

        private void MenuItem_Open_Click(object sender, RoutedEventArgs e)
        {
            this.Show();
        }

        private void MenuItem_Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void myNotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            this.Show();
        }
    }
}
