using System.Windows;
using System.Windows.Input;

namespace ShopfloorManager.Desktop.Behaviors;

/// <summary>Kéo di chuyển Window không viền (WindowStyle=None) bằng cách gắn vào vùng nền title bar.
/// Double-click để Maximize/Restore — thay cho hành vi titlebar mặc định của Windows.</summary>
public static class WindowDragBehavior
{
    public static readonly DependencyProperty IsDragHandleProperty =
        DependencyProperty.RegisterAttached("IsDragHandle", typeof(bool), typeof(WindowDragBehavior),
            new PropertyMetadata(false, OnIsDragHandleChanged));

    public static bool GetIsDragHandle(DependencyObject obj) => (bool)obj.GetValue(IsDragHandleProperty);
    public static void SetIsDragHandle(DependencyObject obj, bool value) => obj.SetValue(IsDragHandleProperty, value);

    private static void OnIsDragHandleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element) return;
        if ((bool)e.NewValue) element.MouseLeftButtonDown += OnMouseLeftButtonDown;
        else element.MouseLeftButtonDown -= OnMouseLeftButtonDown;
    }

    private static void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Window.GetWindow((DependencyObject)sender) is not Window window) return;

        if (e.ClickCount == 2)
        {
            window.WindowState = window.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
            return;
        }

        window.DragMove();
    }
}
