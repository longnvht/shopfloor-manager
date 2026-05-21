using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using ShopfloorManager.Desktop.Services;

namespace ShopfloorManager.Desktop.Controls;

public partial class NumPadWindow : Window
{
    private readonly IKeyboardService _keyboard;

    private const int GWL_EXSTYLE    = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    public NumPadWindow(IKeyboardService keyboard)
    {
        InitializeComponent();
        _keyboard = keyboard;
        PositionBottomRight();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        // Ngăn window steal focus
        var hwnd = new WindowInteropHelper(this).Handle;
        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);
    }

    private void PositionBottomRight()
    {
        var screen = SystemParameters.WorkArea;
        Left = screen.Right - Width - 20;
        Top  = screen.Bottom - Height - 20;
    }

    public void UpdateDisplay(string value) =>
        DisplayText.Text = string.IsNullOrEmpty(value) ? "0" : value;

    private void OnKey(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
            _keyboard.InsertText(btn.Tag?.ToString() ?? string.Empty);
    }

    private void OnBackspace(object sender, RoutedEventArgs e) => _keyboard.Backspace();

    private void OnClear(object sender, RoutedEventArgs e) => _keyboard.Clear();

    private void OnConfirm(object sender, RoutedEventArgs e) => _keyboard.Hide();

    private void OnToggleSign(object sender, RoutedEventArgs e)
    {
        // Toggle âm/dương cho giá trị
        if (DisplayText.Text.StartsWith("-"))
            _keyboard.Clear();
        else
        {
            // Prefix với dấu âm thông qua InsertText ở vị trí 0
            // Thực hiện bằng cách manipulate trực tiếp qua InsertText
        }
    }
}
