using CommunityToolkit.Mvvm.Messaging;
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
using System.Windows.Shapes;
using WindowsScreenTime.Models;
using WindowsScreenTime.ViewModels;

namespace WindowsScreenTime.Views
{
    /// <summary>
    /// AlarmPopup.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AlarmYesOrNoPopup : Window
    {
        public AlarmYesOrNoPopup()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(AlarmYesOrNoPopupViewModel));
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            var message = new TransferDeleteExecute { IsExecute = true };
            WeakReferenceMessenger.Default.Send(message);
            this.Close();
        }
    }
}
