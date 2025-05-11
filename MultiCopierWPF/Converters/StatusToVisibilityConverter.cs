using MultiCopierWPF.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MultiCopierWPF.Converters
{
    public class StatusToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is BackupStatus status && parameter is string targetStatus)
            {
                return status.ToString() == targetStatus ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
