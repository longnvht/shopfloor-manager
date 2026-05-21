using System.Windows.Controls;
using ShopfloorManager.Desktop.ViewModels;

namespace ShopfloorManager.Desktop.Views.Pages;

public partial class JobListPage : UserControl
{
    public JobListPage()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is JobListViewModel vm)
            _ = vm.InitializeAsync();
    }
}
