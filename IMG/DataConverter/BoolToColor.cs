using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace IMG.DataConverter
{
    public static class BoolToColor
    {
        public static Brush ToColor(bool isDuplicate)
        {
            return isDuplicate ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.White);
        }
    }
}
