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

    public string JobHeader => Job is null ? "" :
        $"{Job.JobNumber}  —  {Job.PartNumber} Rev {Job.RevCode}";

    public ObservableCollection<PartOpDto> Operations { get; } = [];

    public Action? OnBack { get; set; }

    public OperationViewModel(IApiClient api)
    {
        _api = api;
    }

    public async Task InitializeAsync(JobSummaryDto job)
    {
        Job = job;
        OnPropertyChanged(nameof(JobHeader));
        await LoadAsync();
    }

    [RelayCommand]
    private void GoBack() => OnBack?.Invoke();

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
        finally
        {
            IsBusy = false;
        }
    }
}
