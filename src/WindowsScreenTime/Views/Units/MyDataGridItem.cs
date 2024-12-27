using Cupertino.Support.UI.Units;
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

namespace WindowsScreenTime.Views.Units
{
    public class MyDataGridItem : DataGridCell
    {
        static MyDataGridItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MyDataGridItem), new FrameworkPropertyMetadata(typeof(MyDataGridItem)));
        }
    }
}
