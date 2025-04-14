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
            var vm = (HomeViewModel)App.Current.Services.GetService(typeof(HomeViewModel));
            vm.OnNavigatedFrom();

            //// 애니메이션 소스 제거
            //ImageBehavior.SetAnimatedSource(MyGif, null);
            //_image = null;

            //// 가비지 컬렉션 힌트
            //GC.Collect(0, GCCollectionMode.Optimized);
        }
    }
}
