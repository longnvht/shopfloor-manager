using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopfloorManager.Desktop.Models;
using ShopfloorManager.Desktop.Services;

namespace ShopfloorManager.Desktop.ViewModels;

public partial class OperationViewModel : Base.ViewModelBase
{
    private readonly IApiClient _api;
    private readonly List<PartOpDto> _allOps = [];

    public JobSummaryDto? Job { get; private set; }

    public string TitleContext => Job is null ? "CHỌN CÔNG ĐOẠN" :
        $"{Job.JobNumber}  ·  {Job.PartNumber} Rev {Job.RevCode}";

    public ObservableCollection<PartOpDto> Operations { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private PartOpDto? _selectedOp;

    [ObservableProperty]
    private string _filterText = string.Empty;

    public Action? OnBack { get; set; }
    public Action<PartOpDto>? OnOperationSelected { get; set; }

    public OperationViewModel(IApiClient api) => _api = api;

    public async Task InitializeAsync(JobSummaryDto job)
    {
        Job = job; FilterText = string.Empty; SelectedOp = null;
        OnPropertyChanged(nameof(TitleContext));
        await LoadAsync();
    }

    [RelayCommand] private void GoBack() => OnBack?.Invoke();

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private void Confirm() { if (SelectedOp is not null) OnOperationSelected?.Invoke(SelectedOp); }

    private bool CanConfirm() => SelectedOp is not null;

    [RelayCommand]
    private void Search() => ApplyFilter();

    partial void OnFilterTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var q = FilterText.Trim().ToLower();
        Operations.Clear();
        foreach (var op in _allOps)
        {
            if (string.IsNullOrEmpty(q) ||
                op.OpNumber.ToLower().Contains(q) ||
                (op.OpTypeName ?? "").ToLower().Contains(q) ||
                (op.Description ?? "").ToLower().Contains(q))
                Operations.Add(op);
        }
    }

    private async Task LoadAsync()
    {
        IsBusy = true; ClearError();
        try
        {
            var result = await _api.GetAsync<List<PartOpDto>>($"/api/v1/jobs/{Job!.Id}/operations");
            _allOps.Clear();
            if (result?.Data is not null) _allOps.AddRange(result.Data);
            ApplyFilter();
            if (!Operations.Any()) ErrorMessage = "Job này chưa có Operation nào.";
        }
        catch (Exception ex) { ErrorMessage = $"Không thể tải: {ex.Message}"; }
        finally { IsBusy = false; }
    }
}
