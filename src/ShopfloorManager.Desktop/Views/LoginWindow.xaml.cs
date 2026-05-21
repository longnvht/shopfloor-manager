using System.Windows;
using ShopfloorManager.Desktop.Configuration;
using ShopfloorManager.Desktop.ViewModels;

namespace ShopfloorManager.Desktop.Views;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel vm, AppSettings settings)
    {
        InitializeComponent();
        DataContext = vm;
        MachineInfoText.Text = string.IsNullOrEmpty(settings.MachineCode)
            ? settings.ApiBaseUrl
            : $"{settings.MachineCode} — {settings.MachineName}";
    }
}
