using CommunityToolkit.Mvvm.Input;
using ShopfloorManager.Desktop.Services;
using ShopfloorManager.Desktop.ViewModels.Base;

namespace ShopfloorManager.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private readonly INavigationService _nav;

    public string WelcomeText => $"Xin chào, {_auth.UserName}";
    public string RoleText => _auth.Role ?? string.Empty;

    public MainViewModel(IAuthService auth, INavigationService nav)
    {
        _auth = auth;
        _nav = nav;
    }

    [RelayCommand]
    private void Logout()
    {
        _auth.Logout();
        _nav.NavigateTo<LoginViewModel>();
    }
}
