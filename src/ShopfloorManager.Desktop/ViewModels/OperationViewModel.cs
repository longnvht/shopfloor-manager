using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopfloorManager.Desktop.Models;
using ShopfloorManager.Desktop.Services;

namespace ShopfloorManager.Desktop.ViewModels;

public partial class OperationViewModel : Base.ViewModelBase
{
    private readonly IApiClient _api;

    public JobSummaryDto? Job { get; private set; }

    public string TitleContext => Job is null ? "CHỌN CÔNG ĐOẠN" :
        $"{Job.JobNumber}  ·  {Job.PartNumber} Rev {Job.RevCode}";

    public ObservableCollection<PartOpDto> Operations { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private PartOpDto? _selectedOp;

    public Action? OnBack { get; set; }
    public Action<PartOpDto>? OnOperationSelected { get; set; }

    public OperationViewModel(IApiClient api) => _api = api;

    public async Task InitializeAsync(JobSummaryDto job)
    {
        Job = job;
        SelectedOp = null;
        OnPropertyChanged(nameof(TitleContext));
        await LoadAsync();
    }

    [RelayCommand]
    private void GoBack() => OnBack?.Invoke();

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private void Confirm()
    {
        if (SelectedOp is not null)
            OnOperationSelected?.Invoke(SelectedOp);
    }

    private bool CanConfirm() => SelectedOp is not null;

    private async Task LoadAsync()
    {
        IsBusy = true;
        ClearError();
        try
        {
            var result = await _api.GetAsync<List<PartOpDto>>($"/api/v1/jobs/{Job!.Id}/operations");
            Operations.Clear();
            if (result?.Data is not null)
                foreach (var op in result.Data)
                    Operations.Add(op);
            if (!Operations.Any())
                ErrorMessage = "Job này chưa có Operation nào.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể tải danh sách Operation: {ex.Message}";
        }
        finally { IsBusy = false; }
    }
}
