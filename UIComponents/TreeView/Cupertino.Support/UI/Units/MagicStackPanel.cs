using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cupertino.Support.UI.Units
{
    public class MagicStackPanel : StackPanel
    {
        private SolidColorBrush color1 = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
        private SolidColorBrush color2 = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EEEEEE"));

        public double ItemHeight
        {
            get { return (double)GetValue(ItemHeightProperty); }
            set { SetValue(ItemHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemHeightProperty =
            DependencyProperty.Register("ItemHeight", typeof(double), typeof(MagicStackPanel), new PropertyMetadata(0.0, ItemHeightPropertyChanged));

        
        // DependencyProperty가 정적이므로 콜백 메서드도 정적으로 선언
        private static void ItemHeightPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // 전달받은 DependencyObject d를 높이값으로 변환해야함
            MagicStackPanel panel = (MagicStackPanel)d;
            panel.Children.Clear();     // 자식 요소들이 계속 추가되어
            panel.Height = panel.ItemHeight;

            int index = (int)panel.ItemHeight / 36;

            for (int i = 0; i < index; i++)
            {
                Border border = new();
                border.Height = 36;
                border.Background = i % 2 == 0 ? panel.color1 : panel.color2;
                panel.Children.Add(border);

            }
        }
    }
}
