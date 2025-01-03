using Jamesnet.Wpf.Animation;
using Jamesnet.Wpf.Controls;
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

namespace NavigationBar
{
    public class MagicBar : ListBox
    {
        ValueItem _vi;
        Storyboard _sb;

        static MagicBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MagicBar), new FrameworkPropertyMetadata(typeof(MagicBar)));
        }

        public override void OnApplyTemplate()      // 바로 Part_circle속성을 사용할 수가 없어서 메서드 사용
        {
            base.OnApplyTemplate();
            Grid circle = (Grid)GetTemplateChild("PART_Circle");

            InitStoryboard(circle);
        }

        private void InitStoryboard(Grid circle)
        {
            _vi = new();
            _sb = new();

            _vi.Mode = EasingFunctionBaseMode.QuinticEaseInOut;
            _vi.Property = new PropertyPath(Canvas.LeftProperty);       //"canvas.LeftProperty"
            _vi.Duration = new Duration(new TimeSpan(0,0,0,1));     //"0.0:0.5"

            Storyboard.SetTarget(_vi, circle);
            Storyboard.SetTargetProperty(_vi, _vi.Property);

            _sb.Children.Add(_vi);          // value item을 storyboard에 추가
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            try
            {
                base.OnSelectionChanged(e);
                _vi.To = SelectedIndex * 80;
                _sb.Begin();
            }
            catch { }
        }
    }
}
