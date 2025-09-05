using System;
using System.Globalization;
using System.Windows.Data;

namespace YMM_REC_Plugin
{
    public class WaveToHeightConverter : IValueConverter
    {
        public double MaxHeight { get; set; } = 50;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is float f)
            {
                return Math.Abs(f) * MaxHeight;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
