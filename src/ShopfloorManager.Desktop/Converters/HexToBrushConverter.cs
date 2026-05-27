using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ShopfloorManager.Desktop.Converters;

[ValueConversion(typeof(string), typeof(Brush))]
public sealed class HexToBrushConverter : IValueConverter
{
    public static readonly HexToBrushConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrEmpty(hex))
        {
            try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
            catch { /* fall through */ }
        }
        return new SolidColorBrush(Color.FromRgb(0x6D, 0x3B, 0x1A)); // BrandPrimary fallback
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
