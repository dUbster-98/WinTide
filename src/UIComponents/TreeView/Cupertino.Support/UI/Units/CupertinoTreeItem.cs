using Cupertino.Support.Local.Models;
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

namespace Cupertino.Support.UI.Units
{
    public class CupertinoTreeItem : DataGridCell
    {


        public ICommand SelectionCommand
        {
            get { return (ICommand)GetValue(SelectionCommandProperty); }
            set { SetValue(SelectionCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectionCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectionCommandProperty =
            DependencyProperty.Register("SelectionCommand", typeof(ICommand), typeof(CupertinoTreeItem), new PropertyMetadata(null));


        static CupertinoTreeItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CupertinoTreeItem), new FrameworkPropertyMetadata(typeof(CupertinoTreeItem)));
        }

        // TreeView의 부모자식 관계 때문에 직접 바인딩이 불가능하여 ICommand 이용
        public CupertinoTreeItem()
        {
            MouseLeftButtonUp += CupertinoTreeItem_MouseLeftButtonUp;
        }

        private void CupertinoTreeItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // 버블링 이벤트 방지
            e.Handled = true;

            // 클릭시, UI의 Datacontext가 FileItem 값에 바인딩되어 있으면 그것을 TreeViewItem으로 간주
            if (DataContext is FileItem item)
            {
                SelectionCommand?.Execute(item);
            }
        }
    }
}
