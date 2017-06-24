using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace WaveformOverlaysPlus.Converters
{
    class ColorConverters
    {
        public static SolidColorBrush GetColorFromHex(string hexColor)
        {
            return new SolidColorBrush(
                Color.FromArgb(Convert.ToByte(hexColor.Substring(1, 2), 16),
                               Convert.ToByte(hexColor.Substring(3, 2), 16),
                               Convert.ToByte(hexColor.Substring(5, 2), 16),
                               Convert.ToByte(hexColor.Substring(7, 2), 16)));
        }
    }
}
