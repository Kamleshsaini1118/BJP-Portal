using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace StockPortalApp.Helpers
{
    public class BoolToGreenBgConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isLowest = value is true;
            return isLowest
                ? (Brush)new BrushConverter().ConvertFrom("#F0FDF4")!
                : (Brush)new BrushConverter().ConvertFrom("#FFFFFF")!;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BoolToGreenBorderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isLowest = value is true;
            return isLowest
                ? (Brush)new BrushConverter().ConvertFrom("#16A34A")!
                : (Brush)new BrushConverter().ConvertFrom("#E2DDD7")!;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
