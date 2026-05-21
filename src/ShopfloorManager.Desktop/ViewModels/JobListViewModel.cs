using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopfloorManager.Desktop.Models;
using ShopfloorManager.Desktop.Services;

namespace ShopfloorManager.Desktop.ViewModels;

public partial class JobListViewModel : Base.ViewModelBase
{
    private readonly IApiClient _api;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _showCompleted;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private JobSummaryDto? _selectedJob;

    public ObservableCollection<JobSummaryDto> Jobs { get; } = [];

    public int PageSize => 20;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool CanPrevPage => CurrentPage > 1;
    public bool CanNextPage => CurrentPage < TotalPages;

    public JobListViewModel(IApiClient api)
    {
        _api = api;
    }

    public async Task InitializeAsync()
    {
        await LoadAsync();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand(CanExecute = nameof(CanPrevPage))]
    private async Task PrevPageAsync()
    {
        CurrentPage--;
        await LoadAsync();
    }

    [RelayCommand(CanExecute = nameof(CanNextPage))]
    private async Task NextPageAsync()
    {
        CurrentPage++;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsBusy = true;
        ClearError();
        try
        {
            var query = BuildQuery();
            var result = await _api.GetAsync<List<JobSummaryDto>>(query);

            Jobs.Clear();
            if (result?.Data is not null)
                foreach (var job in result.Data)
                    Jobs.Add(job);

            TotalCount = result?.Pagination?.Total ?? 0;
            OnPropertyChanged(nameof(TotalPages));
            OnPropertyChanged(nameof(CanPrevPage));
            OnPropertyChanged(nameof(CanNextPage));
            PrevPageCommand.NotifyCanExecuteChanged();
            NextPageCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể tải danh sách Job: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private string BuildQuery()
    {
        var parts = new List<string>
        {
            $"page={CurrentPage}",
            $"pageSize={PageSize}"
        };

        if (!string.IsNullOrWhiteSpace(SearchText))
            parts.Add($"search={Uri.EscapeDataString(SearchText.Trim())}");

        if (!ShowCompleted)
            parts.Add("isComplete=false");

        return "/api/v1/jobs?" + string.Join("&", parts);
    }

    partial void OnShowCompletedChanged(bool value)
    {
        CurrentPage = 1;
        _ = LoadAsync();
    }
}
