using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace WindowsScreenTime.Behaviors
{
    public class MouseBehavior : Behavior<UIElement>
    {
        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(MouseBehavior), new PropertyMetadata(null));

        protected override void OnAttached()
        {
            AssociatedObject.PreviewMouseDown += OnMouseDown;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewMouseDown -= OnMouseDown;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.XButton1 == MouseButtonState.Pressed) // 뒤로가기 버튼 감지
            {
                if (Command?.CanExecute(null) == true)
                {
                    Command.Execute(null);
                }
            }
        }
    }
}
