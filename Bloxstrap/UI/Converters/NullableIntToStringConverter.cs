using System.Globalization;
using System.Windows.Data;

namespace Bloxstrap.UI.Converters
{
    /// <summary>
    /// Two-way binding between <see cref="int?"/> and TextBox text (empty = null).
    /// </summary>
    public sealed class NullableIntToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int i)
                return i.ToString(culture);
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? s = value as string;
            if (string.IsNullOrWhiteSpace(s))
                return (int?)null;

            if (int.TryParse(s.Trim(), NumberStyles.Integer, culture, out int parsed) && parsed > 0)
                return parsed;

            return Binding.DoNothing;
        }
    }
}
