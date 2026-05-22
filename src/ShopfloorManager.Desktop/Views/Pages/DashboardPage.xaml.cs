using System.Windows.Controls;
using System.Windows.Input;
using ShopfloorManager.Desktop.ViewModels;

namespace ShopfloorManager.Desktop.Views.Pages;

public partial class DashboardPage : UserControl
{
    public DashboardPage()
    {
        InitializeComponent();
    }

    private void OnWorkCardTapped(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
            vm.TapWorkInfoCommand.Execute(null);
    }
}
