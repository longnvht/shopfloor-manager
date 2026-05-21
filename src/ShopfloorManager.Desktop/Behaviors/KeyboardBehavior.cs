using System.Windows;
using System.Windows.Controls;

namespace ShopfloorManager.Desktop.Behaviors;

public enum KeyboardMode { None, NumPad, Qwerty }

public static class KeyboardBehavior
{
    public static readonly DependencyProperty ModeProperty =
        DependencyProperty.RegisterAttached(
            "Mode",
            typeof(KeyboardMode),
            typeof(KeyboardBehavior),
            new PropertyMetadata(KeyboardMode.None));

    public static KeyboardMode GetMode(DependencyObject obj) =>
        (KeyboardMode)obj.GetValue(ModeProperty);

    public static void SetMode(DependencyObject obj, KeyboardMode value) =>
        obj.SetValue(ModeProperty, value);
}
