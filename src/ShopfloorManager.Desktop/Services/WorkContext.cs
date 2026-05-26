using CommunityToolkit.Mvvm.ComponentModel;
using ShopfloorManager.Desktop.Models;

namespace ShopfloorManager.Desktop.Services;

public enum AppMode { Operation, View }

/// <summary>
/// Singleton — lưu trạng thái công việc hiện tại của operator.
/// Chia sẻ giữa tất cả Pages thông qua DI.
/// </summary>
public partial class WorkContext : ObservableObject
{
    // ── Operation Mode state ───────────────────────────────────────────
    [ObservableProperty] private JobSummaryDto?          _currentJob;
    [ObservableProperty] private PartOpDto?              _currentOp;
    [ObservableProperty] private ProductWithSessionDto?  _currentProduct;
    [ObservableProperty] private ProductionSessionDto?   _activeSession;

    // ── View Mode browse state — hoàn toàn độc lập với Op context ─────
    [ObservableProperty] private JobSummaryDto?         _viewJob;
    [ObservableProperty] private PartOpDto?             _viewOp;
    [ObservableProperty] private ProductWithSessionDto? _viewProduct;

    public bool HasViewJob     => ViewJob     is not null;
    public bool HasViewOp      => ViewOp      is not null;
    public bool HasViewProduct => ViewProduct is not null;

    /// <summary>Operation = operator đang làm việc bình thường. View = đọc-only do máy đang bị dùng bởi người khác.</summary>
    [ObservableProperty] private AppMode _mode = AppMode.Operation;

    /// <summary>Session của người khác trên máy này (chỉ set khi Mode=View hoặc Leader cần force-finish).</summary>
    public ActiveSessionDto? IncomingSession { get; set; }

    public bool IsOperationMode => Mode == AppMode.Operation;
    public bool IsViewMode      => Mode == AppMode.View;

    public bool HasJob     => CurrentJob     is not null;
    public bool HasOp      => CurrentOp      is not null;
    public bool HasProduct => CurrentProduct is not null;
    public bool IsWip      => ActiveSession?.Status == "open";

    /// <summary>empty | has-job | has-op | wip | complete</summary>
    public string WorkState
    {
        get
        {
            if (!HasJob)     return "empty";
            if (!HasOp)      return "has-job";
            if (!HasProduct) return "has-op";
            return IsWip ? "wip" : "complete";
        }
    }

    partial void OnCurrentJobChanged(JobSummaryDto? value)
    {
        OnPropertyChanged(nameof(HasJob));
        OnPropertyChanged(nameof(WorkState));
    }

    partial void OnCurrentOpChanged(PartOpDto? value)
    {
        OnPropertyChanged(nameof(HasOp));
        OnPropertyChanged(nameof(WorkState));
    }

    partial void OnCurrentProductChanged(ProductWithSessionDto? value)
    {
        OnPropertyChanged(nameof(HasProduct));
        OnPropertyChanged(nameof(WorkState));
    }

    partial void OnActiveSessionChanged(ProductionSessionDto? value)
    {
        OnPropertyChanged(nameof(IsWip));
        OnPropertyChanged(nameof(WorkState));
    }

    partial void OnViewJobChanged(JobSummaryDto? value)     => OnPropertyChanged(nameof(HasViewJob));
    partial void OnViewOpChanged(PartOpDto? value)          => OnPropertyChanged(nameof(HasViewOp));
    partial void OnViewProductChanged(ProductWithSessionDto? value) => OnPropertyChanged(nameof(HasViewProduct));

    partial void OnModeChanged(AppMode value)
    {
        OnPropertyChanged(nameof(IsOperationMode));
        OnPropertyChanged(nameof(IsViewMode));
        ClearViewContext(); // Hai môi trường độc lập — xóa browse state khi chuyển mode
    }

    public void SetJob(JobSummaryDto job)
    {
        CurrentJob     = job;
        CurrentOp      = null;
        CurrentProduct = null;
        ActiveSession  = null;
    }

    public void SetOp(PartOpDto op)
    {
        CurrentOp      = op;
        CurrentProduct = null;
        ActiveSession  = null;
    }

    public void SetProduct(ProductWithSessionDto product, ProductionSessionDto? session = null)
    {
        CurrentProduct = product;
        ActiveSession  = session;
    }

    public void ClearProduct()
    {
        CurrentProduct = null;
        ActiveSession  = null;
    }

    // ── View Mode browse context ───────────────────────────────────────
    public void SetViewJob(JobSummaryDto job) { ViewJob = job; ViewOp = null; ViewProduct = null; }
    public void SetViewOp(PartOpDto op)       { ViewOp = op;   ViewProduct = null; }
    public void SetViewProduct(ProductWithSessionDto product) { ViewProduct = product; }

    public void ClearViewContext()
    {
        ViewJob     = null;
        ViewOp      = null;
        ViewProduct = null;
    }

    public void Clear()
    {
        CurrentJob      = null;
        CurrentOp       = null;
        CurrentProduct  = null;
        ActiveSession   = null;
        IncomingSession = null;
        Mode            = AppMode.Operation;
        // View context cleared via OnModeChanged
    }
}
