using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopfloorManager.Desktop.Models;
using ShopfloorManager.Desktop.Services;

namespace ShopfloorManager.Desktop.ViewModels;

public partial class ProductListViewModel : Base.ViewModelBase
{
    private readonly IApiClient _api;
    private readonly IAuthService _auth;
    private readonly Configuration.AppSettings _settings;

    public JobSummaryDto? Job { get; private set; }
    public PartOpDto? Op { get; private set; }

    public string Header => Job is null || Op is null ? "" :
        $"{Job.JobNumber}  ›  OP {Op.OpNumber} — {Op.OpTypeDisplay}";

    [ObservableProperty]
    private ProductWithSessionDto? _selectedProduct;

    public ObservableCollection<ProductWithSessionDto> Products { get; } = [];

    public Action? OnBack { get; set; }
    public Action<ProductWithSessionDto>? OnProductSelected { get; set; }

    // Timer để cập nhật elapsed time cho WIP products
    private System.Threading.Timer? _timer;

    public ProductListViewModel(IApiClient api, IAuthService auth,
        Configuration.AppSettings settings)
    {
        _api = api;
        _auth = auth;
        _settings = settings;
    }

    public async Task InitializeAsync(JobSummaryDto job, PartOpDto op)
    {
        Job = job;
        Op = op;
        OnPropertyChanged(nameof(Header));
        await LoadAsync();

        // Timer cập nhật elapsed time mỗi 30 giây
        _timer = new System.Threading.Timer(_ =>
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                foreach (var p in Products.Where(p => p.StatusCode == "inprogress"))
                    OnPropertyChanged(nameof(Products)); // trigger refresh
            });
        }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    [RelayCommand]
    private void GoBack()
    {
        _timer?.Dispose();
        OnBack?.Invoke();
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand(CanExecute = nameof(CanSelectProduct))]
    private async Task SelectProductAsync()
    {
        if (SelectedProduct is null || Op is null) return;

        IsBusy = true;
        ClearError();
        try
        {
            var result = await _api.PostAsync<object, object>(
                "/api/v1/production-sessions",
                new
                {
                    productId   = SelectedProduct.ProductId,
                    partOpId    = Op.Id,
                    machineCode = _settings.MachineCode
                });

            if (result?.Success == true)
            {
                OnProductSelected?.Invoke(SelectedProduct);
            }
            else
            {
                ErrorMessage = result?.Error ?? "Không thể chọn sản phẩm.";
                await LoadAsync(); // Refresh để thấy trạng thái mới nhất
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSelectProduct() => SelectedProduct?.IsAvailable == true;

    partial void OnSelectedProductChanged(ProductWithSessionDto? value) =>
        SelectProductCommand.NotifyCanExecuteChanged();

    private async Task LoadAsync()
    {
        if (Job is null || Op is null) return;
        IsBusy = true;
        ClearError();
        try
        {
            var result = await _api.GetAsync<List<ProductWithSessionDto>>(
                $"/api/v1/jobs/{Job.Id}/operations/{Op.Id}/products");

            Products.Clear();
            if (result?.Data is not null)
                foreach (var p in result.Data)
                    Products.Add(p);

            if (!Products.Any())
                ErrorMessage = "Job này chưa có sản phẩm nào.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể tải danh sách sản phẩm: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
