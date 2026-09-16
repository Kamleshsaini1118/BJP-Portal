using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace StockPortalApp.Helpers
{
    /// <summary>
    /// Returns Visible if string is null or empty (showing placeholder text),
    /// and Collapsed if string has content (hiding placeholder text).
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? text = value?.ToString();
            return string.IsNullOrEmpty(text) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
