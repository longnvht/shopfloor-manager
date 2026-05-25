using System.Windows;
using System.Windows.Controls;
using ShopfloorManager.Desktop.ViewModels;

namespace ShopfloorManager.Desktop.Controls;

public partial class NcrDialogWindow : Window
{
    private readonly NcrDialogViewModel _vm;

    public NcrDialogWindow(NcrDialogViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        vm.OnClose = () => Dispatcher.Invoke(Close);
        Loaded += async (_, _) => await vm.LoadAsync();
    }

    private void OnDragHandle_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            DragMove();
    }

    private void OnListBoxRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
    {
        e.Handled = true;
    }
}
