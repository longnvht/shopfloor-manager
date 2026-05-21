using System.Windows;
using ShopfloorManager.Desktop.ViewModels;

namespace ShopfloorManager.Desktop;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.Initialize();
    }
}
