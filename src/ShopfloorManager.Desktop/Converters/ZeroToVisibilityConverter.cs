using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ShopfloorManager.Desktop.Converters;

/// <summary>0 → Visible (empty state), >0 → Collapsed</summary>
[ValueConversion(typeof(int), typeof(Visibility))]
public sealed class ZeroToVisibilityConverter : IValueConverter
{
    public static readonly ZeroToVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int n && n == 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
