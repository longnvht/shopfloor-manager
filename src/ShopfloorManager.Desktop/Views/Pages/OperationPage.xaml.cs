using System.Windows;
using System.Windows.Controls;

namespace ShopfloorManager.Desktop.Views.Pages;

public partial class OperationPage : UserControl
{
    public OperationPage()
    {
        InitializeComponent();
    }

    // Ngăn ListBox tự scroll outer ScrollViewer khi chọn item
    private void OnListBoxRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
    {
        e.Handled = true;
    }
}
