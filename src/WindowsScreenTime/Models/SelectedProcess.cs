using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace WindowsScreenTime.Models
{
    public class SelectedProcess : ObservableObject
    {
        public BitmapImage? ProcessIcon { get; set; }
        public string? ProcessName { get; set; }
    }
}
