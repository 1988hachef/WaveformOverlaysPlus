using System;
using Windows.UI.Xaml.Data;

namespace WaveformOverlaysPlus.Converters
{
    class OpacityBindingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (double)value / 100;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return (int)((double)value * 100);
        }
    }
}
