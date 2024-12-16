using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Cupertino.Support.Local.Converters
{
    public class DepthConverter : MarkupExtension, IValueConverter
    {
        // value로 전달되는 값이 Objcect이므로 int로 변환, Margin값이 Thickness이기 때문에 int(depth)에서 변환하는 과정임
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int depth = (int)value;
            Thickness margin = new Thickness(depth * 20, 0, 0, 0);
            return margin;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        // 여기선 현재값을 반환
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
        
    }
}
