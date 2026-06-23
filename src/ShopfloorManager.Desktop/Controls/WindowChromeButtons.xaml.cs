using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;

namespace ShopfloorManager.Desktop.Controls;

public partial class WindowChromeButtons : UserControl
{
    public WindowChromeButtons()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (Window.GetWindow(this) is not Window window) return;
            UpdateMaximizeIcon(window.WindowState);
            window.StateChanged += (_, _) => UpdateMaximizeIcon(window.WindowState);
        };
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is Window window)
            window.WindowState = WindowState.Minimized;
    }

    private void MaximizeRestore_Click(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is not Window window) return;
        window.WindowState = window.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
        => Window.GetWindow(this)?.Close();

    private void UpdateMaximizeIcon(WindowState state)
        => MaximizeIcon.Kind = state == WindowState.Maximized
            ? PackIconKind.WindowRestore
            : PackIconKind.WindowMaximize;
}
