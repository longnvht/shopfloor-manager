using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ShopfloorManager.Desktop.Behaviors;

public static class DragScrollBehavior
{
    public static readonly DependencyProperty EnabledProperty =
        DependencyProperty.RegisterAttached("Enabled", typeof(bool), typeof(DragScrollBehavior),
            new PropertyMetadata(false, OnEnabledChanged));

    public static bool GetEnabled(DependencyObject obj) => (bool)obj.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject obj, bool value) => obj.SetValue(EnabledProperty, value);

    // Per-instance state stored as attached properties
    private static readonly DependencyProperty StartPointProperty =
        DependencyProperty.RegisterAttached("_StartPoint", typeof(Point), typeof(DragScrollBehavior));
    private static readonly DependencyProperty IsDraggingProperty =
        DependencyProperty.RegisterAttached("_IsDragging", typeof(bool), typeof(DragScrollBehavior));

    private const double DragThreshold = 8.0;

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer sv) return;
        if ((bool)e.NewValue)
        {
            sv.PreviewMouseLeftButtonDown += OnMouseDown;
            sv.PreviewMouseMove           += OnMouseMove;
            sv.PreviewMouseLeftButtonUp   += OnMouseUp;
            sv.MouseLeave                 += OnMouseLeave;
        }
        else
        {
            sv.PreviewMouseLeftButtonDown -= OnMouseDown;
            sv.PreviewMouseMove           -= OnMouseMove;
            sv.PreviewMouseLeftButtonUp   -= OnMouseUp;
            sv.MouseLeave                 -= OnMouseLeave;
        }
    }

    private static void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        var sv = (ScrollViewer)sender;
        sv.SetValue(StartPointProperty, e.GetPosition(sv));
        sv.SetValue(IsDraggingProperty, false);
    }

    private static void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        var sv         = (ScrollViewer)sender;
        var startPoint = (Point)sv.GetValue(StartPointProperty);
        var isDragging = (bool)sv.GetValue(IsDraggingProperty);
        var current    = e.GetPosition(sv);
        var delta      = current - startPoint;

        if (!isDragging && Math.Abs(delta.Y) > DragThreshold)
        {
            isDragging = true;
            sv.SetValue(IsDraggingProperty, true);
            sv.CaptureMouse();
        }

        if (isDragging)
        {
            sv.ScrollToVerticalOffset(sv.VerticalOffset - delta.Y);
            sv.SetValue(StartPointProperty, current);
            e.Handled = true;
        }
    }

    private static void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        var sv         = (ScrollViewer)sender;
        var isDragging = (bool)sv.GetValue(IsDraggingProperty);
        if (isDragging)
        {
            sv.ReleaseMouseCapture();
            e.Handled = true;
        }
        sv.SetValue(IsDraggingProperty, false);
    }

    private static void OnMouseLeave(object sender, MouseEventArgs e)
    {
        var sv = (ScrollViewer)sender;
        sv.ReleaseMouseCapture();
        sv.SetValue(IsDraggingProperty, false);
    }
}
