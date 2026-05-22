using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ShopfloorManager.Desktop.Configuration;
using ShopfloorManager.Desktop.Models;
using ShopfloorManager.Desktop.Services;
using ShopfloorManager.Desktop.ViewModels.Base;

namespace ShopfloorManager.Desktop.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly WorkContext _work;
    private readonly IAuthService _auth;
    private readonly INavigationService _nav;
    private readonly IServiceProvider _sp;
    private readonly AppSettings _settings;
    private DispatcherTimer? _timer;

    // ===== Auth / Machine info =====
    public string UserDisplayName => _auth.UserName ?? "";
    public string UserRole        => _auth.Role ?? "";
    public string MachineCode     => _settings.MachineCode;
    public string MachineName     => _settings.MachineName;
    public string CurrentTime     => DateTime.Now.ToString("HH:mm:ss");

    // ===== Work Info card — state flags =====
    public bool ShowEmpty    => _work.WorkState == "empty";
    public bool ShowHasJob   => _work.WorkState == "has-job";
    public bool ShowHasOp    => _work.WorkState == "has-op";
    public bool ShowWip      => _work.WorkState == "wip";
    public bool ShowComplete => _work.WorkState == "complete";

    // ===== Work Info — display values =====
    public string? JobNumber   => _work.CurrentJob?.JobNumber;
    public string? PartDisplay => _work.CurrentJob is null ? null
        : $"{_work.CurrentJob.PartNumber}  Rev {_work.CurrentJob.RevCode}";
    public string? ShipByDisplay => _work.CurrentJob?.ShipByDisplay;
    public string? RunQtyDisplay => _work.CurrentJob?.RunQty?.ToString();

    public string? OpDisplay    => _work.CurrentOp is null ? null
        : $"OP {_work.CurrentOp.OpNumber} — {_work.CurrentOp.OpTypeDisplay}";
    public string? SetupDisplay => _work.CurrentOp?.SetupTimeDisplay;
    public string? ProdDisplay  => _work.CurrentOp?.ProdTimeDisplay;

    public string? SerialDisplay  => _work.CurrentProduct?.SerialNumber;
    public string? SessionMachine => _work.ActiveSession?.MachineCode;

    [ObservableProperty] private string _elapsedTime = "00:00";

    public ObservableCollection<ShortcutItem> Shortcuts { get; } = [];

    // ===== Navigation action (set by parent) =====
    public Action<string>? NavigateTo { get; set; }

    public DashboardViewModel(WorkContext work, IAuthService auth,
        INavigationService nav, IServiceProvider sp, AppSettings settings)
    {
        _work = work;
        _auth = auth;
        _nav  = nav;
        _sp   = sp;
        _settings = settings;

        _work.PropertyChanged += (_, _) => RefreshWorkInfo();
    }

    public void Initialize()
    {
        RefreshWorkInfo();
        StartClock();
    }

    private void RefreshWorkInfo()
    {
        OnPropertyChanged(nameof(ShowEmpty));
        OnPropertyChanged(nameof(ShowHasJob));
        OnPropertyChanged(nameof(ShowHasOp));
        OnPropertyChanged(nameof(ShowWip));
        OnPropertyChanged(nameof(ShowComplete));
        OnPropertyChanged(nameof(JobNumber));
        OnPropertyChanged(nameof(PartDisplay));
        OnPropertyChanged(nameof(ShipByDisplay));
        OnPropertyChanged(nameof(RunQtyDisplay));
        OnPropertyChanged(nameof(OpDisplay));
        OnPropertyChanged(nameof(SetupDisplay));
        OnPropertyChanged(nameof(ProdDisplay));
        OnPropertyChanged(nameof(SerialDisplay));
        OnPropertyChanged(nameof(SessionMachine));
        RefreshShortcuts();
    }

    private void StartClock()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) =>
        {
            OnPropertyChanged(nameof(CurrentTime));
            UpdateElapsed();
        };
        _timer.Start();
    }

    private void UpdateElapsed()
    {
        if (_work.ActiveSession?.StartedAt is DateTimeOffset started)
        {
            var span = DateTimeOffset.UtcNow - started;
            ElapsedTime = $"{(int)span.TotalMinutes:D2}:{span.Seconds:D2}";
        }
        else
        {
            ElapsedTime = "00:00";
        }
    }

    // ===== Work Info Card tap =====

    [RelayCommand]
    private void TapWorkInfo()
    {
        switch (_work.WorkState)
        {
            case "empty":    NavigateTo?.Invoke("jobs");    break;
            case "has-job":  NavigateTo?.Invoke("ops");     break;
            case "has-op":   NavigateTo?.Invoke("products"); break;
            case "wip":      NavigateTo?.Invoke("fai");     break;
            case "complete": NavigateTo?.Invoke("products"); break;
        }
    }

    [RelayCommand]
    private void SelectJob()    => NavigateTo?.Invoke("jobs");

    [RelayCommand]
    private void GoToFai()      => NavigateTo?.Invoke("fai");

    [RelayCommand]
    private void GoToProducts() => NavigateTo?.Invoke("products");

    // ===== Logout =====

    [RelayCommand]
    private void Logout()
    {
        _timer?.Stop();
        _work.Clear();
        _auth.Logout();
        _nav.NavigateTo<LoginViewModel>();
    }

    // ===== Shortcuts =====

    private void RefreshShortcuts()
    {
        Shortcuts.Clear();
        var role = _auth.Role ?? "";
        var state = _work.WorkState;

        // FAI — khi có product WIP
        if (state == "wip")
            Shortcuts.Add(new ShortcutItem("Tiếp tục FAI", "ClipboardCheck",
                new RelayCommand(() => NavigateTo?.Invoke("fai"))));

        // Chọn Job — luôn có
        Shortcuts.Add(new ShortcutItem("Chọn Job", "ClipboardList",
            new RelayCommand(() => NavigateTo?.Invoke("jobs"))));

        // Chọn sản phẩm — khi có OP
        if (_work.HasOp)
            Shortcuts.Add(new ShortcutItem("Chọn sản phẩm", "FormatListNumbered",
                new RelayCommand(() => NavigateTo?.Invoke("products"))));

        // Tài liệu — khi có OP
        if (_work.HasOp)
        {
            Shortcuts.Add(new ShortcutItem("Xem bản vẽ", "FileImageOutline",
                new RelayCommand(() => NavigateTo?.Invoke("drawing"))));
            Shortcuts.Add(new ShortcutItem("Hướng dẫn gá đặt", "Wrench",
                new RelayCommand(() => NavigateTo?.Invoke("fixture"))));
            Shortcuts.Add(new ShortcutItem("Hướng dẫn công việc", "FileDocumentOutline",
                new RelayCommand(() => NavigateTo?.Invoke("routecard"))));
            Shortcuts.Add(new ShortcutItem("Load G-code", "Download",
                new RelayCommand(() => NavigateTo?.Invoke("gcode"))));
        }

        // QC / Engineer thêm shortcuts
        if (role is "QC Inspector" or "Engineer" or "Administrator")
        {
            if (_work.HasProduct)
                Shortcuts.Add(new ShortcutItem("Lịch sử đo", "ChartBar",
                    new RelayCommand(() => NavigateTo?.Invoke("history"))));

            Shortcuts.Add(new ShortcutItem("Tạo NCR", "AlertCircle",
                new RelayCommand(() => NavigateTo?.Invoke("ncr")),
                IsEnabled: _work.HasProduct));
        }
    }

    public void Cleanup() => _timer?.Stop();
}
