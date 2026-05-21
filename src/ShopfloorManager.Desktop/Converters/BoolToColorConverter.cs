using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ShopfloorManager.Desktop.Converters;

/// <summary>
/// Dùng để highlight dòng overdue: true → đỏ, false → transparent.
/// </summary>
[ValueConversion(typeof(bool), typeof(Brush))]
public sealed class BoolToRedBrushConverter : IValueConverter
{
    public static readonly BoolToRedBrushConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true
            ? new SolidColorBrush(Color.FromRgb(0xFF, 0xEB, 0xEE)) // red-50
            : Brushes.Transparent;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
