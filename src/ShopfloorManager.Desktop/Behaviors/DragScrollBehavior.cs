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

    /// <summary>Hướng kéo-thả — mặc định Vertical (giữ nguyên hành vi các ScrollViewer hiện có).
    /// Set Horizontal cho dải chip cuộn ngang (vd. product switcher trong FaiPage).</summary>
    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.RegisterAttached("Orientation", typeof(Orientation), typeof(DragScrollBehavior),
            new PropertyMetadata(Orientation.Vertical));

    public static Orientation GetOrientation(DependencyObject obj) => (Orientation)obj.GetValue(OrientationProperty);
    public static void SetOrientation(DependencyObject obj, Orientation value) => obj.SetValue(OrientationProperty, value);

    // Per-instance state stored as attached properties
    private static readonly DependencyProperty StartPointProperty =
        DependencyProperty.RegisterAttached("_StartPoint", typeof(Point), typeof(DragScrollBehavior));
    private static readonly DependencyProperty IsDraggingProperty =
        DependencyProperty.RegisterAttached("_IsDragging", typeof(bool), typeof(DragScrollBehavior));

    private const double DragThreshold = 8.0;

    /// <summary>Bỏ qua gesture bắt đầu từ thanh scrollbar/thumb gốc của ScrollViewer — nếu không, kéo-thả
    /// tự viết và ScrollBar.Thumb (native) cùng giành CaptureMouse() giữa lúc kéo, gây giật/nhảy bất thường.</summary>
    private static bool IsFromScrollBar(RoutedEventArgs e)
    {
        var node = e.OriginalSource as DependencyObject;
        while (node is not null)
        {
            if (node is System.Windows.Controls.Primitives.ScrollBar or System.Windows.Controls.Primitives.Thumb)
                return true;
            node = node is System.Windows.Media.Visual or System.Windows.Media.Media3D.Visual3D
                ? System.Windows.Media.VisualTreeHelper.GetParent(node)
                : LogicalTreeHelper.GetParent(node);
        }
        return false;
    }

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
        if (IsFromScrollBar(e)) { sv.SetValue(IsDraggingProperty, false); return; }
        sv.SetValue(StartPointProperty, e.GetPosition(sv));
        sv.SetValue(IsDraggingProperty, false);
    }

    private static void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (IsFromScrollBar(e)) return;
        var sv          = (ScrollViewer)sender;
        var horizontal  = GetOrientation(sv) == Orientation.Horizontal;
        var startPoint  = (Point)sv.GetValue(StartPointProperty);
        var isDragging  = (bool)sv.GetValue(IsDraggingProperty);
        var current     = e.GetPosition(sv);
        var delta       = current - startPoint;
        var dragDistance = horizontal ? delta.X : delta.Y;

        if (!isDragging && Math.Abs(dragDistance) > DragThreshold)
        {
            isDragging = true;
            sv.SetValue(IsDraggingProperty, true);
            sv.CaptureMouse();
        }

        if (isDragging)
        {
            if (horizontal) sv.ScrollToHorizontalOffset(sv.HorizontalOffset - delta.X);
            else             sv.ScrollToVerticalOffset(sv.VerticalOffset - delta.Y);
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
