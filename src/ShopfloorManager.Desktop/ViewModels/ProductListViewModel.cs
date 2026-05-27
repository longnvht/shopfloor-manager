using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopfloorManager.Desktop.Models;
using ShopfloorManager.Desktop.Services;

namespace ShopfloorManager.Desktop.ViewModels;

public partial class ProductListViewModel : Base.ViewModelBase
{
    private readonly IApiClient  _api;
    private readonly WorkContext _work;
    private readonly Configuration.AppSettings _settings;

    private readonly List<ProductWithSessionDto> _allProducts = [];

    public JobSummaryDto? Job { get; private set; }
    public PartOpDto?     Op  { get; private set; }

    public string TitleContext => "CHỌN SẢN PHẨM";
    public string SubContext => Job is null || Op is null ? "" :
        $"{Job.JobNumber}  ·  OP {Op.OpNumber}";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SelectProductCommand))]
    private ProductWithSessionDto? _selectedProduct;

    [ObservableProperty]
    private string _filterText = string.Empty;

    public ObservableCollection<ProductWithSessionDto> Products { get; } = [];

    public Action? OnBack { get; set; }
    public Action<ProductWithSessionDto>? OnProductSelected { get; set; }

    /// <summary>True = View mode — no claiming, "Lựa chọn" button hidden.</summary>
    public bool IsViewMode { get; set; }

    private System.Threading.Timer? _timer;

    public ProductListViewModel(IApiClient api, WorkContext work,
        Configuration.AppSettings settings)
    {
        _api      = api;
        _work     = work;
        _settings = settings;
    }

    public async Task InitializeAsync(JobSummaryDto job, PartOpDto op)
    {
        Job = job; Op = op; FilterText = string.Empty;
        SelectedProduct = null;
        OnPropertyChanged(nameof(SubContext));
        SelectProductCommand.NotifyCanExecuteChanged();
        await LoadAsync();

        _timer = new System.Threading.Timer(_ =>
            App.Current.Dispatcher.Invoke(() => OnPropertyChanged(nameof(Products))),
            null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    // ── Commands ────────────────────────────────────────────────────────

    [RelayCommand]
    private void GoBack() { _timer?.Dispose(); OnBack?.Invoke(); }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private void Search() => ApplyFilter();

    [RelayCommand(CanExecute = nameof(CanSelectProduct))]
    private async Task SelectProductAsync()
    {
        if (SelectedProduct is null) return;

        if (IsViewMode)
        {
            _timer?.Dispose();
            OnProductSelected?.Invoke(SelectedProduct);
            return;
        }

        // Resume: đây là session inprogress của chính mình → navigate thẳng
        if (SelectedProduct.StatusCode == "inprogress"
            && SelectedProduct.SessionId == _work.ActiveSession?.Id)
        {
            _timer?.Dispose();
            OnProductSelected?.Invoke(SelectedProduct);
            return;
        }

        // Inprogress bởi máy/session khác → locked
        if (SelectedProduct.StatusCode == "inprogress")
        {
            ErrorMessage = $"Sản phẩm {SelectedProduct.SerialNumber} đang được gia công trên máy {SelectedProduct.MachineCode}.";
            return;
        }

        // Hoàn thành → locked
        if (SelectedProduct.StatusCode == "complete")
        {
            ErrorMessage = $"Sản phẩm {SelectedProduct.SerialNumber} đã hoàn thành.";
            return;
        }

        // Available (hoặc claimed legacy) → chỉ set WorkContext, không gọi API
        _work.SetProduct(SelectedProduct, null);
        _timer?.Dispose();
        OnProductSelected?.Invoke(SelectedProduct);
        await Task.CompletedTask;
    }

    private bool CanSelectProduct() => SelectedProduct is not null;

    // ── Filter ──────────────────────────────────────────────────────────

    partial void OnFilterTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var q = FilterText.Trim().ToLower();
        Products.Clear();
        foreach (var p in _allProducts)
        {
            if (string.IsNullOrEmpty(q) || p.SerialNumber.ToLower().Contains(q))
                Products.Add(p);
        }
    }

    // ── Load ────────────────────────────────────────────────────────────

    private async Task LoadAsync()
    {
        if (Job is null || Op is null) return;
        IsBusy = true; ClearError();
        try
        {
            var result = await _api.GetAsync<List<ProductWithSessionDto>>(
                $"/api/v1/jobs/{Job.Id}/operations/{Op.Id}/products");

            _allProducts.Clear();
            if (result?.Data is not null) _allProducts.AddRange(result.Data);
            ApplyFilter();

            if (!Products.Any()) ErrorMessage = "Job này chưa có sản phẩm nào.";
        }
        catch (Exception ex) { ErrorMessage = $"Không thể tải: {ex.Message}"; }
        finally { IsBusy = false; }
    }
}
