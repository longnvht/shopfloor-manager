using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using ShopfloorManager.Desktop.Models;
using ShopfloorManager.Desktop.Services;
using ShopfloorManager.Desktop.ViewModels.Base;

namespace ShopfloorManager.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IServiceProvider _sp;
    private readonly WorkContext _work;
    private readonly INavigationService _nav;

    [ObservableProperty]
    private ViewModelBase? _currentPage;

    public MainViewModel(IServiceProvider sp, WorkContext work, INavigationService nav)
    {
        _sp   = sp;
        _work = work;
        _nav  = nav;
    }

    public void Initialize() => NavigateToDashboard();

    // ===== Dashboard (home) =====

    public void NavigateToDashboard()
    {
        var vm = _sp.GetRequiredService<DashboardViewModel>();
        vm.NavigateTo = HandleDashboardNavigation;
        vm.Initialize();
        CurrentPage = vm;
    }

    private void HandleDashboardNavigation(string target)
    {
        switch (target)
        {
            case "jobs":     NavigateToJobs();      break;
            case "ops":      NavigateToOps();       break;
            case "products": NavigateToProducts();  break;
            case "fai":      NavigateToFai();       break;
        }
    }

    // ===== Job List =====

    public void NavigateToJobs()
    {
        var vm = _sp.GetRequiredService<JobListViewModel>();
        vm.OnJobOpened = job =>
        {
            _work.SetJob(job);
            NavigateToOps();
        };
        vm.OnBack = NavigateToDashboard;
        CurrentPage = vm;
        _ = vm.InitializeAsync();
    }

    // ===== Operation List =====

    public void NavigateToOps()
    {
        if (_work.CurrentJob is null) { NavigateToJobs(); return; }
        var vm = _sp.GetRequiredService<OperationViewModel>();
        vm.OnBack = NavigateToDashboard;
        vm.OnOperationSelected = op =>
        {
            _work.SetOp(op);
            NavigateToProducts();
        };
        CurrentPage = vm;
        _ = vm.InitializeAsync(_work.CurrentJob);
    }

    // ===== Product List =====

    public void NavigateToProducts()
    {
        if (_work.CurrentJob is null || _work.CurrentOp is null) { NavigateToDashboard(); return; }
        var vm = _sp.GetRequiredService<ProductListViewModel>();
        vm.OnBack = NavigateToDashboard;
        vm.OnProductSelected = product =>
        {
            _work.SetProduct(product);
            NavigateToDashboard(); // FAI page sẽ implement sau
        };
        CurrentPage = vm;
        _ = vm.InitializeAsync(_work.CurrentJob, _work.CurrentOp);
    }

    // ===== FAI (placeholder) =====

    public void NavigateToFai()
    {
        // TODO: implement FAIPage
        NavigateToDashboard();
    }

    // ===== Logout (called từ DashboardViewModel) =====

    public void HandleLogout()
    {
        _work.Clear();
        _nav.NavigateTo<LoginViewModel>();
    }
}
