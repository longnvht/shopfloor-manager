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
    private readonly IKeyboardService _keyboard;

    [ObservableProperty]
    private ViewModelBase? _currentPage;

    public MainViewModel(IServiceProvider sp, WorkContext work, INavigationService nav, IKeyboardService keyboard)
    {
        _sp       = sp;
        _work     = work;
        _nav      = nav;
        _keyboard = keyboard;
    }

    public void Initialize() => NavigateToDashboard();

    // ===== Dashboard (home) =====

    public void NavigateToDashboard()
    {
        _keyboard.Hide();
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
            // document viewers & settings wired in future tasks
        }
    }

    // ===== Job List =====

    public void NavigateToJobs()
    {
        _keyboard.Hide();
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
        _keyboard.Hide();
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
        _keyboard.Hide();
        if (_work.CurrentJob is null || _work.CurrentOp is null) { NavigateToDashboard(); return; }
        var vm = _sp.GetRequiredService<ProductListViewModel>();
        vm.OnBack = NavigateToDashboard;
        // WorkContext already updated by ProductListViewModel (SetProduct with session)
        vm.OnProductSelected = _ => NavigateToDashboard();
        CurrentPage = vm;
        _ = vm.InitializeAsync(_work.CurrentJob, _work.CurrentOp);
    }

    // ===== FAI (Bảng đo) =====

    public void NavigateToFai()
    {
        _keyboard.Hide();
        if (_work.CurrentJob is null || _work.CurrentOp is null || _work.CurrentProduct is null)
        {
            NavigateToDashboard();
            return;
        }
        var vm = _sp.GetRequiredService<FaiViewModel>();
        vm.OnBack = NavigateToDashboard;
        CurrentPage = vm;
        _ = vm.InitializeAsync();
    }

    // ===== Logout (called từ DashboardViewModel) =====

    public void HandleLogout()
    {
        _work.Clear();
        _nav.NavigateTo<LoginViewModel>();
    }
}
