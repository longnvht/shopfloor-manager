using CommunityToolkit.Mvvm.ComponentModel;
using ShopfloorManager.Desktop.Models;

namespace ShopfloorManager.Desktop.Services;

/// <summary>
/// Singleton — lưu trạng thái công việc hiện tại của operator.
/// Chia sẻ giữa tất cả Pages thông qua DI.
/// </summary>
public partial class WorkContext : ObservableObject
{
    [ObservableProperty] private JobSummaryDto?          _currentJob;
    [ObservableProperty] private PartOpDto?              _currentOp;
    [ObservableProperty] private ProductWithSessionDto?  _currentProduct;
    [ObservableProperty] private ProductionSessionDto?   _activeSession;

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

    public void Clear()
    {
        CurrentJob     = null;
        CurrentOp      = null;
        CurrentProduct = null;
        ActiveSession  = null;
    }
}
