using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ShopfloorManager.Desktop.Services;
using ShopfloorManager.Desktop.ViewModels.Base;

namespace ShopfloorManager.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private readonly INavigationService _nav;
    private readonly IServiceProvider _sp;

    [ObservableProperty]
    private ViewModelBase? _currentPage;

    [ObservableProperty]
    private string _pageTitle = "Danh sách Job";

    public string WelcomeText => $"Xin chào, {_auth.UserName}";
    public string RoleText => _auth.Role ?? string.Empty;

    public MainViewModel(IAuthService auth, INavigationService nav, IServiceProvider sp)
    {
        _auth = auth;
        _nav = nav;
        _sp = sp;
    }

    public void Initialize()
    {
        NavigateToJobs();
    }

    [RelayCommand]
    private void NavigateToJobs()
    {
        PageTitle = "Danh sách Job";
        CurrentPage = _sp.GetRequiredService<JobListViewModel>();
    }

    [RelayCommand]
    private void Logout()
    {
        _auth.Logout();
        _nav.NavigateTo<LoginViewModel>();
    }
}
