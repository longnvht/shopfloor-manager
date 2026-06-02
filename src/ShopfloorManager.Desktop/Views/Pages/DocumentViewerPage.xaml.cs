using System.Windows;
using System.Windows.Controls;
using ShopfloorManager.Desktop.ViewModels;

namespace ShopfloorManager.Desktop.Views.Pages;

public partial class DocumentViewerPage : UserControl
{
    private bool _webViewReady;

    public DocumentViewerPage()
    {
        InitializeComponent();
    }

    // Fires when WebView2 becomes visible (IsPdfViewerVisible = true → Grid shows → WebView2 shows).
    // Initialize the underlying Edge process on first use, then navigate to the presigned URL.
    private async void PdfWebView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!(bool)e.NewValue) return;
        if (DataContext is not DocumentViewerViewModel vm || vm.PdfUrl is null) return;

        try
        {
            if (!_webViewReady)
            {
                await PdfWebView.EnsureCoreWebView2Async();

                // Enable touch zoom (pinch) and scroll for the Edge PDF viewer.
                // IsZoomControlEnabled: allows user to change zoom via keyboard / mouse wheel / pinch.
                // IsPinchZoomEnabled:   enables 2-finger pinch-to-zoom gesture on touch screen.
                var settings = PdfWebView.CoreWebView2.Settings;
                settings.IsZoomControlEnabled = true;
                settings.IsPinchZoomEnabled   = true;

                _webViewReady = true;
            }
            PdfWebView.ZoomFactor = 1.0;   // reset zoom khi chuyển tài liệu mới
            PdfWebView.Source = new Uri(vm.PdfUrl);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WebView2 navigation failed: {ex.Message}");
        }
    }
}
