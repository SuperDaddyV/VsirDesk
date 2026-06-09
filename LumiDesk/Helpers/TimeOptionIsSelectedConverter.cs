using System.Globalization;
using System.Windows.Data;

namespace LumiDesk.Helpers;

public class TimeOptionIsSelectedConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not string slot || values[1] is not DateTime dateTime)
        {
            return false;
        }

        return string.Equals(slot, dateTime.ToString("HH:mm", culture), StringComparison.Ordinal);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
