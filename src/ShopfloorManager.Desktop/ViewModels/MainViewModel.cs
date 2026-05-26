using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using ShopfloorManager.Desktop.Configuration;
using ShopfloorManager.Desktop.Controls;
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
    private readonly AppSettings _settings;

    [ObservableProperty]
    private ViewModelBase? _currentPage;

    public MainViewModel(IServiceProvider sp, WorkContext work, INavigationService nav, IKeyboardService keyboard, AppSettings settings)
    {
        _sp       = sp;
        _work     = work;
        _nav      = nav;
        _keyboard = keyboard;
        _settings = settings;
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

    private bool IsViewMode => _work.IsViewMode;

    // ===== Job List =====

    public void NavigateToJobs()
    {
        _keyboard.Hide();
        var vm = _sp.GetRequiredService<JobListViewModel>();
        vm.OnJobOpened = job =>
        {
            // Write WorkContext when no active session so shortcuts update in View Mode too
            if (!IsViewMode || _work.ActiveSession is null) _work.SetJob(job);
            _browseJob = job;
            NavigateToOps();
        };
        vm.OnBack = NavigateToDashboard;
        CurrentPage = vm;
        _ = vm.InitializeAsync();
    }

    // Temporary browse state used in View_Mode to pass Job/Op context without writing WorkContext
    private JobSummaryDto? _browseJob;
    private PartOpDto? _browseOp;

    // ===== Operation List =====

    public void NavigateToOps()
    {
        _keyboard.Hide();
        var job = IsViewMode ? _browseJob : _work.CurrentJob;
        if (job is null) { NavigateToJobs(); return; }
        var vm = _sp.GetRequiredService<OperationViewModel>();
        vm.OnBack = NavigateToDashboard;
        vm.OnOperationSelected = op =>
        {
            if (!IsViewMode || _work.ActiveSession is null) _work.SetOp(op);
            _browseOp = op;
            NavigateToProducts();
        };
        CurrentPage = vm;
        _ = vm.InitializeAsync(job);
    }

    // ===== Product List =====

    public void NavigateToProducts()
    {
        _keyboard.Hide();
        var job = IsViewMode ? _browseJob : _work.CurrentJob;
        var op  = IsViewMode ? _browseOp  : _work.CurrentOp;
        if (job is null || op is null) { NavigateToDashboard(); return; }
        var vm = _sp.GetRequiredService<ProductListViewModel>();
        vm.OnBack = NavigateToDashboard;
        if (IsViewMode)
        {
            // In View_Mode: no claiming — just browse and return
            vm.OnProductSelected = _ => NavigateToDashboard();
            vm.IsViewMode = true;
        }
        else
        {
            vm.OnProductSelected = _ => NavigateToDashboard();
            vm.IsViewMode = false;
        }
        CurrentPage = vm;
        _ = vm.InitializeAsync(job, op);
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
        vm.OnDimensionFail = ShowNcrDialog;
        CurrentPage = vm;
        _ = vm.InitializeAsync();
    }

    private void ShowNcrDialog(NcrTriggerArgs args)
    {
        var api    = _sp.GetRequiredService<IApiClient>();
        var dialogVm = new NcrDialogViewModel(api, args);
        var dialog = new NcrDialogWindow(dialogVm);
        dialog.ShowDialog();
    }

    // ===== Logout (called từ DashboardViewModel) =====

    public void HandleLogout()
    {
        _work.Clear();
        _nav.NavigateTo<LoginViewModel>();
    }
}
