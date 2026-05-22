using System.Windows;
using ShopfloorManager.Desktop.ViewModels;

namespace ShopfloorManager.Desktop;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        Loaded += (_, _) => vm.Initialize();
    }
}
