using System;
using System.Collections.Generic;
using System.IO;
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
using WpfAnimatedGif;

namespace WindowsScreenTime.Views
{
    /// <summary>
    /// Home.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Home : UserControl
    {
        private static BitmapImage _image;
        private WeakReference<BitmapImage> _gifImageRef;

        public Home()
        {
            InitializeComponent();
            //DataContext = App.Current.Services.GetService(typeof(HomeViewModel));

            _image = new BitmapImage();
            _image.BeginInit();
            _image.UriSource = new Uri($"{Directory.GetCurrentDirectory()}/Resources/pic.gif");
            _image.DecodePixelWidth = 300; // 실제 표시되는 크기로 제한
            _image.DecodePixelHeight = 300;
            _image.CacheOption = BitmapCacheOption.OnLoad; // 메모리에 완전히 로드
            _image.EndInit();
            _image.Freeze();

            _gifImageRef = new WeakReference<BitmapImage>(_image);

            if (_gifImageRef != null && _gifImageRef.TryGetTarget(out var image))
            {
                ImageBehavior.SetAnimatedSource(MyGif, image);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is HomeViewModel vm)
            {
                vm.OnNavigatedTo();
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is HomeViewModel vm)
            {
                vm.OnNavigatedFrom();
            }

            // 애니메이션 소스 제거
            ImageBehavior.SetAnimatedSource(MyGif, null);
            _image = null;

            // 가비지 컬렉션 힌트
            GC.Collect(0, GCCollectionMode.Optimized);
        }
    }
}
