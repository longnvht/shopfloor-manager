using System.Collections.ObjectModel;
using System.Windows.Media;
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
    private readonly ISignalRService _signalR;

    // ── Tracking ───────────────────────────────────────────────────────
    private readonly DateTimeOffset _appStartTime  = DateTimeOffset.Now;
    private readonly DateTimeOffset _loginTime     = DateTimeOffset.Now;
    private TimeSpan  _totalActiveTime = TimeSpan.Zero;
    private int       _productsCreated   = 0;
    private int       _productsCompleted = 0;

    // ── Daily summary (OEE data) ───────────────────────────────────────
    private DailySummaryDto? _dailySummary;

    // ── SignalR notification banner ────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSignalRNotification))]
    private string? _signalRNotification;
    public bool HasSignalRNotification => SignalRNotification is not null;
    private DispatcherTimer? _notifyTimer;

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

    // ── Mode-aware context helpers ─────────────────────────────────────
    // Đọc đúng context tùy mode: Operation → CurrentJob/Op/Product, View → ViewJob/Op/Product
    private JobSummaryDto?         CtxJob     => _work.IsViewMode ? _work.ViewJob     : _work.CurrentJob;
    private PartOpDto?             CtxOp      => _work.IsViewMode ? _work.ViewOp      : _work.CurrentOp;
    private ProductWithSessionDto? CtxProduct => _work.IsViewMode ? _work.ViewProduct : _work.CurrentProduct;

    // ── Work Info card ─────────────────────────────────────────────────
    public bool HasWork      => CtxJob is not null;
    public bool HasSession   => !_work.IsViewMode && _work.ActiveSession is not null;
    // "Tiếp tục" — có job/op nhưng chưa chọn product
    public bool CanNavigate  => HasWork && !_work.HasProduct && _work.ActiveSession is null;
    public bool IsWip        => _work.IsWip;
    // "Bắt đầu" — đã chọn product nhưng chưa có session active
    public bool CanStart     => _work.HasProduct && !_work.IsWip && _work.IsOperationMode;
    public bool CanStop      => _work.IsWip && _work.IsOperationMode;

    // ── Mutually exclusive button visibility ───────────────────────────────
    /// <summary>Hiện "Chọn Job" chỉ khi chưa có job VÀ không đang cần force-finish.</summary>
    public bool ShowSelectJobButton => !HasWork && !CanForceFinish;
    /// <summary>Hiện "Tiếp tục" khi có thể navigate nhưng không đang force-finish.</summary>
    public bool ShowNavigateButton  => CanNavigate && !CanForceFinish;

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

    /// <summary>Có thể toggle mode: false khi máy đang bị dùng bởi người khác VÀ role không phải Leader/Admin.</summary>
    public bool CanSwitchMode =>
        _work.IncomingSession is null
        || _work.IncomingSession.ClaimedBy == _auth.UserId
        || _auth.Role is "Leader" or "Manager" or "Administrator";

    /// <summary>Hiện nút "Kết thúc" bình thường — ẩn khi đang hiện ForceFinish.</summary>
    public bool ShowStopButton => CanStop && !CanForceFinish;

    public string? JobNumber    => CtxJob?.JobNumber;
    public string? PartDisplay  => CtxJob is null ? null
        : $"{CtxJob.PartNumber}  Rev {CtxJob.RevCode}";
    public string? OpDisplay    => CtxOp is null ? null
        : $"OP {CtxOp.OpNumber} — {CtxOp.OpTypeDisplay}";
    public string? SerialDisplay => CtxProduct?.SerialNumber;
    public string? StatusDisplay => !_work.IsViewMode ? _work.CurrentProduct?.DisplayStatus : null;

    [ObservableProperty] private string _elapsedTime = "00:00";

    // ── OEE — Availability (từ in-memory session timing) ─────────────
    public double AvailabilityPct
    {
        get
        {
            var uptime = (DateTimeOffset.Now - _appStartTime).TotalMinutes;
            if (uptime < 1) return 0;
            var active = (_totalActiveTime + CurrentActiveSpan).TotalMinutes;
            return Math.Min(100, active / uptime * 100);
        }
    }

    public SolidColorBrush AvailabilityBrush => OeeBrush(AvailabilityPct);

    // ── OEE — Quality (từ daily summary) ─────────────────────────────
    public double QualityPct    => _dailySummary?.QualityPct ?? 0;
    public bool   HasQualityData => (_dailySummary?.TotalMeasured ?? 0) > 0;
    public SolidColorBrush QualityBrush => OeeBrush(QualityPct);

    // ── Job Progress ──────────────────────────────────────────────────
    public double JobProgressPct
    {
        get
        {
            var job = CtxJob;
            if (job is null || job.RunQty is null or 0) return 0;
            return Math.Min(100, job.CompletedCount * 100.0 / job.RunQty.Value);
        }
    }

    public string JobProgressText
    {
        get
        {
            var job = CtxJob;
            if (job is null) return string.Empty;
            if (job.RunQty is null) return $"{job.CompletedCount} sp";
            return $"{job.CompletedCount}/{job.RunQty} sp";
        }
    }

    public bool HasJobProgress => CtxJob is not null;

    private static SolidColorBrush OeeBrush(double pct) => pct switch
    {
        >= 80 => new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32)),  // green
        >= 60 => new SolidColorBrush(Color.FromRgb(0xF5, 0x7F, 0x17)),  // amber
        _     => new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)),  // red
    };

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
        IApiClient api, INavigationService nav, AppSettings settings, ISignalRService signalR)
    {
        _work     = work;
        _auth     = auth;
        _api      = api;
        _nav      = nav;
        _settings = settings;
        _signalR  = signalR;

        _work.PropertyChanged += (_, _) => RefreshWorkInfo();
        _signalR.NcrCreated += OnNcrCreated;
    }

    private void OnNcrCreated(NcrCreatedPayload ncr)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var serial = ncr.SerialNumber is not null ? $"  S/N {ncr.SerialNumber}" : "";
            SignalRNotification = $"NCR mới: {ncr.NcrNumber}  —  {ncr.JobNumber}{serial}";

            _notifyTimer?.Stop();
            _notifyTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
            _notifyTimer.Tick += (_, _) => { SignalRNotification = null; _notifyTimer.Stop(); };
            _notifyTimer.Start();
        });
    }

    public override void Cleanup()
    {
        _timer?.Stop();
        _notifyTimer?.Stop();
        _signalR.NcrCreated -= OnNcrCreated;
    }

    public void Initialize()
    {
        RefreshWorkInfo();
        StartClock();
        _ = LoadDailySummaryAsync();
    }

    private async Task LoadDailySummaryAsync()
    {
        try
        {
            var result = await _api.GetAsync<DailySummaryDto>(
                $"/api/v1/machines/{_settings.MachineCode}/daily-summary");
            if (result?.Success == true && result.Data is not null)
            {
                _dailySummary = result.Data;
                OnPropertyChanged(nameof(QualityPct));
                OnPropertyChanged(nameof(QualityBrush));
                OnPropertyChanged(nameof(HasQualityData));
            }
        }
        catch { /* non-critical — dashboard still works without it */ }
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
        OnPropertyChanged(nameof(AvailabilityPct));
        OnPropertyChanged(nameof(AvailabilityBrush));
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
        OnPropertyChanged(nameof(CanSwitchMode));
        OnPropertyChanged(nameof(ShowStopButton));
        OnPropertyChanged(nameof(ShowSelectJobButton));
        OnPropertyChanged(nameof(ShowNavigateButton));
        OnPropertyChanged(nameof(JobProgressPct));
        OnPropertyChanged(nameof(JobProgressText));
        OnPropertyChanged(nameof(HasJobProgress));
        StartCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
        ForceFinishCommand.NotifyCanExecuteChanged();
        ToggleModeCommand.NotifyCanExecuteChanged();
        RefreshShortcuts();
    }

    // ── Work Info commands ─────────────────────────────────────────────

    [RelayCommand]
    private void TapWorkInfo()
    {
        if (_work.IsViewMode)
        {
            // View Mode: navigate dựa trên view context
            if (!_work.HasViewOp)       NavigateTo?.Invoke("ops");
            else if (!_work.HasViewProduct) NavigateTo?.Invoke("products");
            else                        NavigateTo?.Invoke("products");
            return;
        }
        switch (_work.WorkState)
        {
            case "empty":       NavigateTo?.Invoke("jobs");     break;
            case "has-job":     NavigateTo?.Invoke("ops");      break;
            case "has-op":      NavigateTo?.Invoke("products"); break;
            case "has-product":
            case "wip":         NavigateTo?.Invoke("products"); break;
        }
    }

    [RelayCommand]
    private void SelectJob() => NavigateTo?.Invoke("jobs");

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartAsync()
    {
        if (_work.CurrentProduct is null || _work.CurrentOp is null) return;
        IsBusy = true; ClearError();
        try
        {
            var result = await _api.PostAsync<object, ProductionSessionDto>(
                "/api/v1/production-sessions",
                new { productId = _work.CurrentProduct.ProductId,
                      partOpId  = _work.CurrentOp.Id,
                      machineCode = _settings.MachineCode });
            if (result?.Success == true && result.Data is not null)
            {
                _work.SetProduct(_work.CurrentProduct, result.Data);
                RefreshWorkInfo();
            }
            else
            {
                ErrorMessage = result?.Error ?? "Không thể bắt đầu phiên gia công.";
            }
        }
        catch (Exception ex) { ErrorMessage = $"Lỗi: {ex.Message}"; }
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
                _ = LoadDailySummaryAsync();
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

    // ── Mode toggle ────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanSwitchMode))]
    private void ToggleMode()
    {
        _work.Mode = _work.IsViewMode ? AppMode.Operation : AppMode.View;
        RefreshWorkInfo();
    }

    // ── Logout ─────────────────────────────────────────────────────────

    [RelayCommand]
    private void Logout()
    {
        Cleanup();
        _work.Clear();
        _auth.Logout();
        _ = _signalR.DisconnectAsync();
        _nav.NavigateTo<LoginViewModel>();
    }

    // ── Shortcuts ──────────────────────────────────────────────────────

    private void RefreshShortcuts()
    {
        Shortcuts.Clear();
        var role = _auth.Role ?? "";

        // Dùng view context khi View Mode để shortcuts phản ánh đúng trạng thái browse
        bool hasJob  = _work.IsViewMode ? _work.HasViewJob  : _work.HasJob;
        bool hasOp   = _work.IsViewMode ? _work.HasViewOp   : _work.HasOp;
        bool hasProd = _work.IsViewMode ? _work.HasViewProduct : _work.HasProduct;
        // FAI chỉ khả dụng trong Operation Mode khi session đã được bắt đầu (StartedAt != null)
        bool canFai  = !_work.IsViewMode && hasProd && _work.ActiveSession?.StartedAt.HasValue == true;

        // Operation Mode + inprogress → hiển thị mờ, không cho thay đổi context
        bool canChangeContext = _work.IsViewMode || !_work.IsWip;
        Add("Chọn Job",       "ClipboardList",       "jobs",      always: true,  isEnabled: canChangeContext);
        Add("Chọn OP",        "PlaylistEdit",        "ops",       when: hasJob,  isEnabled: canChangeContext);
        Add("Chọn sản phẩm",  "FormatListNumbered",  "products",  when: hasOp,   isEnabled: canChangeContext);
        Add("Xem bản vẽ",     "FileImageOutline",    "drawing",   when: hasOp);
        Add("Hướng dẫn gá",   "Wrench",              "fixture",   when: hasOp);
        Add("Hướng dẫn CW",   "FileDocumentOutline", "routecard", when: hasOp);
        Add("Xem G-code",     "Download",            "gcode",     when: hasOp);
        Add("Bảng đo",        "ClipboardTextOutline","fai",       when: canFai);
        bool canQcInline = !_work.IsViewMode && hasProd
            && _work.CurrentProduct?.StatusCode == "complete";
        if (role is "QC Inspector" or "Administrator")
        {
            Add("FAI Final",  "ClipboardCheckOutline","fai-final",  when: canFai);
            Add("QC Inline",  "Magnify",               "qc-inline", when: canQcInline);
        }
        if (role is "QC Inspector" or "Engineer" or "Administrator")
        {
            Add("Lịch sử đo", "ChartBar",   "history", when: hasProd && !_work.IsViewMode);
            Add("Tạo NCR",    "AlertCircle","ncr",     when: hasProd && !_work.IsViewMode);
        }
        if (role is "Administrator")
        {
            Add("Cài đặt",    "CogOutline", "settings", always: true);
        }
    }

    private void Add(string title, string icon, string target,
        bool always = false, bool when = false, bool isEnabled = true)
    {
        if (!always && !when) return;
        Shortcuts.Add(new ShortcutItem(title, icon,
            new RelayCommand(() => NavigateTo?.Invoke(target)), isEnabled));
    }

    // Track product claimed
    public void OnProductClaimed() { _productsCreated++; OnPropertyChanged(nameof(ProductsCreatedDisplay)); }
}
