using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopfloorManager.Desktop.Configuration;
using ShopfloorManager.Desktop.Models;
using ShopfloorManager.Desktop.Services;
using ShopfloorManager.Desktop.ViewModels.Base;

namespace ShopfloorManager.Desktop.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly WorkContext  _work;
    private readonly IAuthService _auth;
    private readonly IApiClient   _api;
    private readonly INavigationService _nav;
    private readonly AppSettings  _settings;

    // ── Tracking ───────────────────────────────────────────────────────
    private readonly DateTimeOffset _appStartTime  = DateTimeOffset.Now;
    private readonly DateTimeOffset _loginTime     = DateTimeOffset.Now;
    private TimeSpan  _totalActiveTime = TimeSpan.Zero;
    private int       _productsCreated   = 0;
    private int       _productsCompleted = 0;

    // ── Timer ──────────────────────────────────────────────────────────
    private DispatcherTimer? _timer;

    // ── Clock ──────────────────────────────────────────────────────────
    public string CurrentTime => DateTime.Now.ToString("HH:mm:ss");

    // ── Machine card ───────────────────────────────────────────────────
    public string MachineCode     => _settings.MachineCode;
    public string MachineName     => _settings.MachineName;

    public string UptimeDisplay
    {
        get { var t = DateTimeOffset.Now - _appStartTime; return FormatSpan(t); }
    }

    public string ActiveTimeDisplay => FormatSpan(_totalActiveTime + CurrentActiveSpan);
    public string IdleTimeDisplay
    {
        get
        {
            var idle = (DateTimeOffset.Now - _appStartTime) - (_totalActiveTime + CurrentActiveSpan);
            return FormatSpan(idle < TimeSpan.Zero ? TimeSpan.Zero : idle);
        }
    }
    public string ProductsCompletedDisplay => _productsCompleted.ToString();

    // ── Operator card ──────────────────────────────────────────────────
    public string UserDisplayName  => _auth.UserName ?? "";
    public string UserRole         => _auth.Role ?? "";
    public string CheckInDisplay   => _loginTime.LocalDateTime.ToString("HH:mm");
    public string WorkDuration     => FormatSpan(DateTimeOffset.Now - _loginTime);
    public string OperatorIdleDisplay
    {
        get
        {
            var idle = (DateTimeOffset.Now - _loginTime) - (_totalActiveTime + CurrentActiveSpan);
            return FormatSpan(idle < TimeSpan.Zero ? TimeSpan.Zero : idle);
        }
    }
    public string ProductsCreatedDisplay => _productsCreated.ToString();

    // ── Work Info card ─────────────────────────────────────────────────
    public bool HasWork      => _work.HasJob;
    public bool HasSession   => _work.ActiveSession is not null;
    public bool CanNavigate  => _work.HasJob && _work.ActiveSession is null && _work.IsOperationMode;
    public bool IsWip        => _work.IsWip;
    public bool CanStart     => _work.IsWip && !(_work.ActiveSession?.StartedAt.HasValue == true) && _work.IsOperationMode;
    public bool CanStop      => _work.IsWip &&   _work.ActiveSession?.StartedAt.HasValue == true  && _work.IsOperationMode;

    // ── View mode / Force-finish ───────────────────────────────────────
    public bool IsViewMode      => _work.IsViewMode;
    public bool IsOperationMode => _work.IsOperationMode;

    /// <summary>Tên người đang giữ session trên máy này (hiển thị trong banner View mode).</summary>
    public string IncomingOwnerName => _work.IncomingSession?.ClaimedByName ?? string.Empty;

    /// <summary>Leader/Admin có thể force-finish session của người khác (không phải của chính mình).</summary>
    public bool CanForceFinish =>
        _work.IncomingSession is not null
        && _work.IncomingSession.ClaimedBy != _auth.UserId
        && _auth.Role is "Leader" or "Manager" or "Administrator";

    /// <summary>Hiện nút "Kết thúc" bình thường — ẩn khi đang hiện ForceFinish.</summary>
    public bool ShowStopButton => CanStop && !CanForceFinish;

    public string? JobNumber    => _work.CurrentJob?.JobNumber;
    public string? PartDisplay  => _work.CurrentJob is null ? null
        : $"{_work.CurrentJob.PartNumber}  Rev {_work.CurrentJob.RevCode}";
    public string? OpDisplay    => _work.CurrentOp is null ? null
        : $"OP {_work.CurrentOp.OpNumber} — {_work.CurrentOp.OpTypeDisplay}";
    public string? SerialDisplay => _work.CurrentProduct?.SerialNumber;
    public string? StatusDisplay => _work.CurrentProduct?.DisplayStatus;

    [ObservableProperty] private string _elapsedTime = "00:00";

    private TimeSpan CurrentActiveSpan
    {
        get
        {
            if (_work.ActiveSession?.StartedAt is DateTimeOffset s)
                return DateTimeOffset.UtcNow - s;
            return TimeSpan.Zero;
        }
    }

    // ── Shortcuts ──────────────────────────────────────────────────────
    public ObservableCollection<ShortcutItem> Shortcuts { get; } = [];

    // ── Navigation callback ────────────────────────────────────────────
    public Action<string>? NavigateTo { get; set; }

    public DashboardViewModel(WorkContext work, IAuthService auth,
        IApiClient api, INavigationService nav, AppSettings settings)
    {
        _work     = work;
        _auth     = auth;
        _api      = api;
        _nav      = nav;
        _settings = settings;

        _work.PropertyChanged += (_, _) => RefreshWorkInfo();
    }

    public void Initialize()
    {
        RefreshWorkInfo();
        StartClock();
    }

    // ── Clock / stats update ───────────────────────────────────────────

    private void StartClock()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
    }

    private void Tick()
    {
        OnPropertyChanged(nameof(CurrentTime));
        OnPropertyChanged(nameof(UptimeDisplay));
        OnPropertyChanged(nameof(ActiveTimeDisplay));
        OnPropertyChanged(nameof(IdleTimeDisplay));
        OnPropertyChanged(nameof(WorkDuration));
        OnPropertyChanged(nameof(OperatorIdleDisplay));
        UpdateElapsed();
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

    private static string FormatSpan(TimeSpan t) =>
        t.TotalHours >= 1
            ? $"{(int)t.TotalHours:D2}:{t.Minutes:D2}:{t.Seconds:D2}"
            : $"{t.Minutes:D2}:{t.Seconds:D2}";

    // ── Work Info refresh ──────────────────────────────────────────────

    private void RefreshWorkInfo()
    {
        OnPropertyChanged(nameof(HasWork));
        OnPropertyChanged(nameof(HasSession));
        OnPropertyChanged(nameof(CanNavigate));
        OnPropertyChanged(nameof(IsWip));
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(CanStop));
        OnPropertyChanged(nameof(JobNumber));
        OnPropertyChanged(nameof(PartDisplay));
        OnPropertyChanged(nameof(OpDisplay));
        OnPropertyChanged(nameof(SerialDisplay));
        OnPropertyChanged(nameof(StatusDisplay));
        OnPropertyChanged(nameof(IsViewMode));
        OnPropertyChanged(nameof(IsOperationMode));
        OnPropertyChanged(nameof(IncomingOwnerName));
        OnPropertyChanged(nameof(CanForceFinish));
        OnPropertyChanged(nameof(ShowStopButton));
        StartCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
        ForceFinishCommand.NotifyCanExecuteChanged();
        RefreshShortcuts();
    }

    // ── Work Info commands ─────────────────────────────────────────────

    [RelayCommand]
    private void TapWorkInfo()
    {
        switch (_work.WorkState)
        {
            case "empty":    NavigateTo?.Invoke("jobs");     break;
            case "has-job":  NavigateTo?.Invoke("ops");      break;
            case "has-op":   NavigateTo?.Invoke("products"); break;
            case "wip":
            case "complete": NavigateTo?.Invoke("products"); break;
        }
    }

    [RelayCommand]
    private void SelectJob() => NavigateTo?.Invoke("jobs");

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartAsync()
    {
        if (_work.ActiveSession is null) return;
        IsBusy = true;
        try
        {
            var result = await _api.PutAsync<object, ProductionSessionDto>(
                $"/api/v1/production-sessions/{_work.ActiveSession.Id}/start", new { });
            if (result?.Data is not null)
            {
                _work.SetProduct(_work.CurrentProduct!, result.Data);
                RefreshWorkInfo();
            }
        }
        finally { IsBusy = false; }
    }

    [RelayCommand(CanExecute = nameof(CanStop))]
    private async Task StopAsync()
    {
        if (_work.ActiveSession is null) return;
        IsBusy = true;
        try
        {
            var result = await _api.PutAsync<object, ProductionSessionDto>(
                $"/api/v1/production-sessions/{_work.ActiveSession.Id}/complete", new { });
            if (result?.Data is not null)
            {
                // Cộng thời gian active
                if (_work.ActiveSession.StartedAt.HasValue)
                    _totalActiveTime += DateTimeOffset.UtcNow - _work.ActiveSession.StartedAt.Value;
                _productsCompleted++;
                OnPropertyChanged(nameof(ProductsCompletedDisplay));
                _work.ClearProduct();
                RefreshWorkInfo();
            }
        }
        finally { IsBusy = false; }
    }

    // ── Force-finish (Leader/Admin kết thúc session của người khác) ────

    [RelayCommand(CanExecute = nameof(CanForceFinish))]
    private async Task ForceFinishAsync()
    {
        if (_work.IncomingSession is not { SessionId: var sid }) return;
        IsBusy = true;
        try
        {
            var result = await _api.PutAsync<object, ProductionSessionDto>(
                $"/api/v1/production-sessions/{sid}/force-complete", new { });
            if (result?.Success == true)
            {
                _work.IncomingSession = null;
                _work.Mode = AppMode.Operation;
                _work.Clear();
                RefreshWorkInfo();
            }
            else
            {
                ErrorMessage = result?.Error ?? "Không thể kết thúc phiên.";
            }
        }
        finally { IsBusy = false; }
    }

    // ── Logout ─────────────────────────────────────────────────────────

    [RelayCommand]
    private void Logout()
    {
        _timer?.Stop();
        _work.Clear();
        _auth.Logout();
        _nav.NavigateTo<LoginViewModel>();
    }

    // ── Shortcuts ──────────────────────────────────────────────────────

    private void RefreshShortcuts()
    {
        Shortcuts.Clear();
        var role  = _auth.Role ?? "";
        var state = _work.WorkState;

        Add("Chọn Job",       "ClipboardList",       "jobs",      always: true);
        Add("Chọn OP",        "PlaylistEdit",        "ops",       when: _work.HasJob);
        Add("Chọn sản phẩm",  "FormatListNumbered",  "products",  when: _work.HasOp);
        Add("Xem bản vẽ",     "FileImageOutline",    "drawing",   when: _work.HasOp);
        Add("Hướng dẫn gá",   "Wrench",              "fixture",   when: _work.HasOp);
        Add("Hướng dẫn CW",   "FileDocumentOutline", "routecard", when: _work.HasOp);
        Add("Xem G-code",     "Download",            "gcode",     when: _work.HasOp);
        Add("Bảng đo",        "ClipboardTextOutline","fai",       when: _work.HasProduct);
        if (role is "QC Inspector" or "Engineer" or "Administrator")
        {
            Add("Lịch sử đo", "ChartBar",   "history", when: _work.HasProduct);
            Add("Tạo NCR",    "AlertCircle","ncr",     when: _work.HasProduct);
        }
        if (role is "Administrator")
        {
            Add("Cài đặt",    "CogOutline", "settings", always: true);
        }
    }

    private void Add(string title, string icon, string target,
        bool always = false, bool when = false)
    {
        if (!always && !when) return;
        Shortcuts.Add(new ShortcutItem(title, icon,
            new RelayCommand(() => NavigateTo?.Invoke(target))));
    }

    public void Cleanup() => _timer?.Stop();

    // Track product claimed
    public void OnProductClaimed() { _productsCreated++; OnPropertyChanged(nameof(ProductsCreatedDisplay)); }
}
