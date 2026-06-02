using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ShopfloorManager.Desktop.Behaviors;

namespace ShopfloorManager.Desktop.Controls;

public partial class ToolpathCanvasControl : System.Windows.Controls.UserControl
{
    // ── Dependency property ───────────────────────────────────────────
    public static readonly DependencyProperty MovesProperty =
        DependencyProperty.Register(
            nameof(Moves), typeof(IList<ToolpathMove>),
            typeof(ToolpathCanvasControl),
            new PropertyMetadata(null, (d, _) => ((ToolpathCanvasControl)d).RebuildAndFit()));

    public IList<ToolpathMove>? Moves
    {
        get => (IList<ToolpathMove>?)GetValue(MovesProperty);
        set => SetValue(MovesProperty, value);
    }

    // ── State ─────────────────────────────────────────────────────────
    private readonly MatrixTransform _transform = new();
    private Point _panStart;
    private bool  _panning;
    private bool  _fitted;

    public ToolpathCanvasControl()
    {
        InitializeComponent();

        DrawCanvas.RenderTransform = _transform;
        RapidPath.StrokeDashArray  = new DoubleCollection { 4, 3 };

        RootGrid.MouseWheel           += OnMouseWheel;
        RootGrid.MouseLeftButtonDown  += OnMouseLeftDown;
        RootGrid.MouseMove            += OnMouseMove;
        RootGrid.MouseLeftButtonUp    += OnMouseLeftUp;
        RootGrid.MouseRightButtonDown += OnMouseRightDown;

        Loaded      += (_, _) => TryFitToView();
        SizeChanged += (_, _) => TryFitToView();
    }

    // ── Rebuild ───────────────────────────────────────────────────────
    private void RebuildAndFit()
    {
        BuildGeometry();
        _fitted = false;
        TryFitToView();
    }

    private void TryFitToView()
    {
        if (_fitted || ActualWidth < 1) return;
        FitToView();
    }

    // ── Geometry ──────────────────────────────────────────────────────
    private void BuildGeometry()
    {
        var rapidGeo = new StreamGeometry();
        var cutGeo   = new StreamGeometry();
        int nRapid = 0, nCut = 0;

        using (var rc = rapidGeo.Open())
        using (var cc = cutGeo.Open())
        {
            if (Moves is not null)
            {
                foreach (var m in Moves)
                {
                    bool rapid = m.Type == MoveType.Rapid;
                    AppendMove(rapid ? rc : cc, m);
                    if (rapid) nRapid++; else nCut++;
                }
            }
        }

        rapidGeo.Freeze();
        cutGeo.Freeze();
        RapidPath.Data = rapidGeo;
        CutPath.Data   = cutGeo;

        int total = nRapid + nCut;
        InfoText.Text = total == 0
            ? "Không có dữ liệu toolpath"
            : $"Rapid: {nRapid}   Cut: {nCut}   Total: {total}";
    }

    private static void AppendMove(StreamGeometryContext ctx, ToolpathMove m)
    {
        // Y-flip: G-code Y-up → screen Y-down
        var p1 = new Point(m.X1, -m.Y1);
        var p2 = new Point(m.X2, -m.Y2);
        ctx.BeginFigure(p1, false, false);

        if (m.Type is MoveType.ArcCW or MoveType.ArcCCW)
        {
            double r    = Math.Sqrt(Math.Pow(m.X1 - m.CX, 2) + Math.Pow(m.Y1 - m.CY, 2));
            double span = GcodeToolpathParser.ArcSpan(m);

            // Y-flip swaps CW↔CCW visually; compensate by using opposite sweep direction.
            // Because the direction is inverted, what was a "small" arc (< π) in G-code
            // becomes the "large" arc in the flipped direction.
            var  sweep   = m.Type == MoveType.ArcCW
                ? SweepDirection.Counterclockwise
                : SweepDirection.Clockwise;
            bool isLarge = span < Math.PI;

            ctx.ArcTo(p2, new Size(r, r), 0, isLarge, sweep, true, false);
        }
        else
        {
            ctx.LineTo(p2, true, false);
        }
    }

    // ── Fit to view ───────────────────────────────────────────────────
    private Rect ComputeScreenBounds()
    {
        if (Moves is null || Moves.Count == 0) return new Rect(0, 0, 100, 100);

        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;

        void Expand(double x, double y)
        {
            if (x < minX) minX = x; if (x > maxX) maxX = x;
            if (y < minY) minY = y; if (y > maxY) maxY = y;
        }

        foreach (var m in Moves)
        {
            // All coordinates already Y-flipped (screen space)
            Expand(m.X1, -m.Y1);
            Expand(m.X2, -m.Y2);
            if (m.Type is MoveType.ArcCW or MoveType.ArcCCW)
            {
                double r = Math.Sqrt(Math.Pow(m.X1 - m.CX, 2) + Math.Pow(m.Y1 - m.CY, 2));
                Expand(m.CX - r, -(m.CY + r));
                Expand(m.CX + r, -(m.CY - r));
            }
        }

        if (minX > maxX) return new Rect(0, 0, 100, 100);
        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    private void FitToView()
    {
        var bounds = ComputeScreenBounds();
        double w = ActualWidth, h = ActualHeight;
        if (w < 1 || h < 1 || bounds.Width < 1e-9 || bounds.Height < 1e-9) return;

        const double pad = 0.88;
        double scale = Math.Min((w * pad) / bounds.Width, (h * pad) / bounds.Height);
        double cx = bounds.X + bounds.Width  / 2;
        double cy = bounds.Y + bounds.Height / 2;

        _transform.Matrix = new Matrix(scale, 0, 0, scale,
            w / 2 - scale * cx,
            h / 2 - scale * cy);
        _fitted = true;
    }

    // ── Mouse: zoom (scroll wheel) ────────────────────────────────────
    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var    cursor = e.GetPosition(RootGrid);
        double f      = e.Delta > 0 ? 1.15 : 1.0 / 1.15;
        var    m      = _transform.Matrix;

        _transform.Matrix = new Matrix(
            m.M11 * f, m.M12 * f,
            m.M21 * f, m.M22 * f,
            m.OffsetX * f + cursor.X * (1 - f),
            m.OffsetY * f + cursor.Y * (1 - f));

        e.Handled = true;
    }

    // ── Mouse: pan (left-drag) ────────────────────────────────────────
    private void OnMouseLeftDown(object sender, MouseButtonEventArgs e)
    {
        _panStart = e.GetPosition(RootGrid);
        _panning  = true;
        RootGrid.CaptureMouse();
        e.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_panning) return;
        var pos   = e.GetPosition(RootGrid);
        var delta = pos - _panStart;
        _panStart = pos;

        var m = _transform.Matrix;
        m.Translate(delta.X, delta.Y);
        _transform.Matrix = m;
    }

    private void OnMouseLeftUp(object sender, MouseButtonEventArgs e)
    {
        if (!_panning) return;
        _panning = false;
        RootGrid.ReleaseMouseCapture();
    }

    // ── Mouse: right-click → re-fit ───────────────────────────────────
    private void OnMouseRightDown(object sender, MouseButtonEventArgs e)
    {
        FitToView();
        e.Handled = true;
    }
}
