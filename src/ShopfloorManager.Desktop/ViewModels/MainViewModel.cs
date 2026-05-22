using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ShopfloorManager.Desktop.Models;
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
        var vm = _sp.GetRequiredService<JobListViewModel>();
        vm.OnJobOpened = NavigateToOperations;
        CurrentPage = vm;
        _ = vm.InitializeAsync();
    }

    private void NavigateToOperations(JobSummaryDto job)
    {
        PageTitle = $"Operations — {job.JobNumber}";
        var vm = _sp.GetRequiredService<OperationViewModel>();
        vm.OnBack = NavigateToJobs;
        vm.OnOperationSelected = op => NavigateToProducts(job, op);
        CurrentPage = vm;
        _ = vm.InitializeAsync(job);
    }

    private void NavigateToProducts(JobSummaryDto job, PartOpDto op)
    {
        PageTitle = $"Sản phẩm — {job.JobNumber} › OP {op.OpNumber}";
        var vm = _sp.GetRequiredService<ProductListViewModel>();
        vm.OnBack = () => NavigateToOperations(job);
        CurrentPage = vm;
        _ = vm.InitializeAsync(job, op);
    }

    [RelayCommand]
    private void Logout()
    {
        _auth.Logout();
        _nav.NavigateTo<LoginViewModel>();
    }
}
