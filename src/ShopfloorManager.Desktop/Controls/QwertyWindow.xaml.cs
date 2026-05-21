using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using ShopfloorManager.Desktop.Services;

namespace ShopfloorManager.Desktop.Controls;

public partial class QwertyWindow : Window
{
    private readonly IKeyboardService _keyboard;

    private const int GWL_EXSTYLE      = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    public QwertyWindow(IKeyboardService keyboard)
    {
        InitializeComponent();
        _keyboard = keyboard;
        Loaded += (_, _) => PositionBottom();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);
    }

    private void PositionBottom()
    {
        var screen = SystemParameters.WorkArea;
        Left = (screen.Width - ActualWidth) / 2;
        Top  = screen.Bottom - ActualHeight - 20;
    }

    // ===== KEY PRESS =====

    private void OnKey(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var ch = btn.Tag?.ToString() ?? string.Empty;

        // Áp dụng CAPS cho chữ cái
        if (ch.Length == 1 && char.IsLetter(ch[0]))
            ch = CapsBtn.IsChecked == true ? ch.ToUpper() : ch.ToLower();

        _keyboard.InsertText(ch);
    }

    private void OnBackspace(object sender, RoutedEventArgs e) => _keyboard.Backspace();
    private void OnClear(object sender, RoutedEventArgs e) => _keyboard.Clear();
    private void OnConfirm(object sender, RoutedEventArgs e) => _keyboard.Hide();
    private void OnClose(object sender, RoutedEventArgs e) => _keyboard.Hide();

    // ===== CAPS LOCK =====

    private void OnCapsChanged(object sender, RoutedEventArgs e)
    {
        var caps = CapsBtn.IsChecked == true;
        // Cập nhật label tất cả key chữ cái trong QwertyPanel
        UpdateLetterKeys(QwertyPanel, caps);
    }

    private static void UpdateLetterKeys(UIElement parent, bool caps)
    {
        foreach (var btn in FindButtons(parent))
        {
            var tag = btn.Tag?.ToString() ?? string.Empty;
            if (tag.Length == 1 && char.IsLetter(tag[0]))
                btn.Content = caps ? tag.ToUpper() : tag.ToLower();
        }
    }

    private static IEnumerable<Button> FindButtons(UIElement element)
    {
        if (element is Panel panel)
            foreach (UIElement child in panel.Children)
                foreach (var btn in FindButtons(child))
                    yield return btn;
        else if (element is Button btn)
            yield return btn;
    }

    // ===== 123 / ABC TOGGLE =====

    private void OnShowNumeric(object sender, RoutedEventArgs e)
    {
        QwertyPanel.Visibility  = Visibility.Collapsed;
        NumericPanel.Visibility = Visibility.Visible;
        // Sync trạng thái toggle button phía ABC panel
        if (AbcToggleBtn.IsChecked == true)
            AbcToggleBtn.IsChecked = false;
        PositionBottom();
    }

    private void OnShowQwerty(object sender, RoutedEventArgs e)
    {
        NumericPanel.Visibility = Visibility.Collapsed;
        QwertyPanel.Visibility  = Visibility.Visible;
        // Sync trạng thái toggle button phía 123 panel
        if (NumToggleBtn.IsChecked == true)
            NumToggleBtn.IsChecked = false;
        PositionBottom();
    }
}
