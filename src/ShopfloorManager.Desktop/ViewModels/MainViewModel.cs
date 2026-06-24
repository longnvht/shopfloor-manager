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

    private void SetPage(ViewModelBase vm)
    {
        CurrentPage?.Cleanup();
        CurrentPage = vm;
    }

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
        SetPage(vm);
    }

    private void HandleDashboardNavigation(string target)
    {
        switch (target)
        {
            case "jobs":     NavigateToJobs();          break;
            case "ops":      NavigateToOps();           break;
            case "products": NavigateToProducts();      break;
            case "fai":      NavigateToFai();           break;
            case "fai-final": NavigateToFaiFinal();    break;
            case "qc-inline": NavigateToQcInline();    break;
            case "gcode":
            case "drawing":
            case "fixture":
            case "routecard":
                NavigateToDocumentViewer(); break;
            case "settings": NavigateToSettings();      break;
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
            if (IsViewMode) _work.SetViewJob(job);
            else _work.SetJob(job);
            _browseJob = job;
            NavigateToOps();
        };
        vm.OnBack = NavigateToDashboard;
        SetPage(vm);
        _ = vm.InitializeAsync();
    }

    // Browse state — dùng cho View_Mode để truyền context mà không ghi WorkContext chính
    private JobSummaryDto? _browseJob;
    private PartOpDto? _browseOp;

    // ===== Operation List =====

    public void NavigateToOps()
    {
        _keyboard.Hide();
        var job = IsViewMode ? _browseJob ?? _work.ViewJob : _work.CurrentJob;
        if (job is null) { NavigateToJobs(); return; }
        var vm = _sp.GetRequiredService<OperationViewModel>();
        vm.OnBack = NavigateToDashboard;
        vm.OnOperationSelected = op =>
        {
            if (IsViewMode) _work.SetViewOp(op);
            else _work.SetOp(op);
            _browseOp = op;
            NavigateToProducts();
        };
        SetPage(vm);
        _ = vm.InitializeAsync(job);
    }

    // ===== Product List =====

    public void NavigateToProducts()
    {
        _keyboard.Hide();
        var job = IsViewMode ? _browseJob ?? _work.ViewJob : _work.CurrentJob;
        var op  = IsViewMode ? _browseOp  ?? _work.ViewOp  : _work.CurrentOp;
        if (job is null || op is null) { NavigateToDashboard(); return; }
        var vm = _sp.GetRequiredService<ProductListViewModel>();
        vm.OnBack = NavigateToDashboard;
        if (IsViewMode)
        {
            vm.OnProductSelected = product =>
            {
                _work.SetViewProduct(product);
                NavigateToDashboard();
            };
            vm.IsViewMode = true;
        }
        else
        {
            vm.OnProductSelected = _ => NavigateToDashboard();
            vm.IsViewMode = false;
        }
        SetPage(vm);
        _ = vm.InitializeAsync(job, op);
    }

    // ===== FAI (Bảng đo) =====

    public void NavigateToFai()
    {
        _keyboard.Hide();
        if (_work.CurrentJob is null || _work.CurrentOp is null || _work.CurrentProduct is null
            || _work.ActiveSession?.StartedAt.HasValue != true)
        {
            NavigateToDashboard();
            return;
        }
        var vm = _sp.GetRequiredService<FaiViewModel>();
        vm.OnBack = NavigateToDashboard;
        vm.OnDimensionFail = ShowNcrDialog;
        SetPage(vm);
        _ = vm.InitializeAsync();
    }

    // ===== FAI Final (re-inspect sau rework) =====

    public void NavigateToFaiFinal()
    {
        _keyboard.Hide();
        // QC Inspector luôn ở View Mode (không tạo session) — không thể yêu cầu ActiveSession.StartedAt.
        var job     = _work.IsViewMode ? _work.ViewJob     : _work.CurrentJob;
        var op      = _work.IsViewMode ? _work.ViewOp      : _work.CurrentOp;
        var product = _work.IsViewMode ? _work.ViewProduct : _work.CurrentProduct;
        if (job is null || op is null || product is null)
        {
            NavigateToDashboard();
            return;
        }
        var vm = _sp.GetRequiredService<FaiViewModel>();
        vm.Mode = FaiMode.Final;
        vm.OnBack = NavigateToDashboard;
        vm.OnDimensionFail = ShowNcrDialog;
        SetPage(vm);
        _ = vm.InitializeAsync();
    }

    // ===== QC Inline (QC kiểm ngẫu nhiên trên sản phẩm đã hoàn thành OP) =====

    public void NavigateToQcInline()
    {
        _keyboard.Hide();
        // QC Inspector luôn ở View Mode (không tạo session) — đọc View*, không phải Current*.
        var job     = _work.IsViewMode ? _work.ViewJob     : _work.CurrentJob;
        var op      = _work.IsViewMode ? _work.ViewOp      : _work.CurrentOp;
        var product = _work.IsViewMode ? _work.ViewProduct : _work.CurrentProduct;
        if (job is null || op is null || product is null || product.StatusCode != "complete")
        {
            NavigateToDashboard();
            return;
        }
        var vm = _sp.GetRequiredService<FaiViewModel>();
        vm.Mode = FaiMode.QcInline;
        vm.OnBack = NavigateToDashboard;
        vm.OnDimensionFail = ShowNcrDialog;
        SetPage(vm);
        _ = vm.InitializeAsync();
    }

    // ===== Document Viewer =====

    public void NavigateToDocumentViewer()
    {
        _keyboard.Hide();
        var job = IsViewMode ? _browseJob ?? _work.ViewJob : _work.CurrentJob;
        var op  = IsViewMode ? _browseOp  ?? _work.ViewOp  : _work.CurrentOp;
        if (job is null || op is null) { NavigateToDashboard(); return; }
        var vm = _sp.GetRequiredService<DocumentViewerViewModel>();
        vm.OnBack = NavigateToDashboard;
        SetPage(vm);
        _ = vm.InitializeAsync(job, op);
    }

    // ===== Settings =====

    public void NavigateToSettings()
    {
        _keyboard.Hide();
        var vm = _sp.GetRequiredService<SettingsViewModel>();
        vm.OnBack = NavigateToDashboard;
        vm.Initialize();
        SetPage(vm);
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
