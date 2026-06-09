using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopfloorManager.Desktop.Configuration;
using ShopfloorManager.Desktop.Models;
using ShopfloorManager.Desktop.Services;

namespace ShopfloorManager.Desktop.ViewModels;

public partial class FaiViewModel : Base.ViewModelBase
{
    private readonly IApiClient  _api;
    private readonly WorkContext _work;
    private readonly AppSettings _settings;

    public JobSummaryDto?          Job     { get; private set; }
    public PartOpDto?              Op      { get; private set; }
    public ProductWithSessionDto?  Product { get; private set; }

    /// <summary>
    /// true = FAI Final mode: re-inspect sau rework, chỉ hiển thị Fail dims,
    /// chỉ QC Inspector, lưu với IsFinal=true.
    /// </summary>
    public bool IsFinalMode { get; set; }

    public string PageTitle => IsFinalMode
        ? "NHẬP KẾT QUẢ ĐO (FAI FINAL)"
        : "NHẬP KẾT QUẢ ĐO (FAI)";

    public string TitleContext => Product is null ? "" :
        $"{Job?.JobNumber}  ·  OP {Op?.OpNumber}  ·  S/N: {Product.SerialNumber}";

    public ObservableCollection<DimensionCardVm> Dimensions { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPlaceholder))]
    [NotifyPropertyChangedFor(nameof(ShowNumericInput))]
    [NotifyPropertyChangedFor(nameof(ShowTextInput))]
    [NotifyPropertyChangedFor(nameof(IsInputLocked))]
    [NotifyPropertyChangedFor(nameof(IsInputEnabled))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    [NotifyCanExecuteChangedFor(nameof(SetPassCommand))]
    [NotifyCanExecuteChangedFor(nameof(SetFailCommand))]
    private DimensionCardVm? _selectedDimension;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string _inputValue = "";

    public int    MeasuredCount => Dimensions.Count(d => d.IsMeasured);
    public int    TotalCount    => Dimensions.Count;
    public double Progress      => TotalCount > 0 ? (double)MeasuredCount / TotalCount : 0;

    public bool ShowPlaceholder  => SelectedDimension is null;
    public bool ShowNumericInput => SelectedDimension is { IsTextType: false };
    public bool ShowTextInput    => SelectedDimension is { IsTextType: true };
    // Normal FAI: lock sau bất kỳ lần đo nào.
    // FAI Final: chỉ lock khi Pass (Fail dims vẫn cho phép đo lại).
    public bool IsInputLocked    => IsFinalMode
        ? SelectedDimension?.State == MeasureState.Pass
        : SelectedDimension?.IsMeasured == true;
    public bool IsInputEnabled   => !IsInputLocked;

    public Action? OnBack { get; set; }
    public Action<NcrTriggerArgs>? OnDimensionFail { get; set; }

    public FaiViewModel(IApiClient api, WorkContext work, AppSettings settings)
    {
        _api      = api;
        _work     = work;
        _settings = settings;
    }

    public async Task InitializeAsync()
    {
        if (_work.CurrentJob is null || _work.CurrentOp is null || _work.CurrentProduct is null)
        {
            OnBack?.Invoke();
            return;
        }

        Job     = _work.CurrentJob;
        Op      = _work.CurrentOp;
        Product = _work.CurrentProduct;
        OnPropertyChanged(nameof(TitleContext));

        await LoadAsync();
    }

    partial void OnSelectedDimensionChanged(DimensionCardVm? value)
    {
        if (value?.IsMeasured == true && !value.IsTextType)
            InputValue = value.MeasuredValue?.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) ?? "";
        else
            InputValue = "";
    }

    private async Task LoadAsync()
    {
        IsBusy = true; ClearError();
        try
        {
            var resp = await _api.GetAsync<FaiSheetResponse>(
                $"/api/v1/fai?jobId={Job!.Id}&partOpId={Op!.Id}");

            Dimensions.Clear();
            if (resp?.Data is null)
            {
                ErrorMessage = "Không tải được danh sách kích thước.";
                return;
            }

            var row = resp.Data.Rows?.FirstOrDefault(r => r.ProductId == Product!.ProductId);

            foreach (var dim in resp.Data.Dimensions ?? [])
            {
                var cell = row?.Cells?.FirstOrDefault(c => c.BalloonNumber == dim.BalloonNumber);
                var state = cell?.Result switch
                {
                    "Pass" => MeasureState.Pass,
                    "Fail" => MeasureState.Fail,
                    _      => MeasureState.Unmeasured
                };
                Dimensions.Add(new DimensionCardVm
                {
                    Id            = dim.Id,
                    BalloonNumber = dim.BalloonNumber,
                    NominalValue  = dim.NominalValue,
                    TolerancePlus = dim.TolerancePlus,
                    ToleranceMinus = dim.ToleranceMinus,
                    MaxValue      = dim.MaxValue,
                    MinValue      = dim.MinValue,
                    Unit          = dim.Unit ?? "",
                    IsTextType    = dim.IsTextType,
                    NominalText   = dim.NominalText,
                    IsFinal       = dim.IsFinal,
                    IsCritical    = dim.IsCritical,
                    State         = state,
                    MeasuredValue = cell?.Value
                });
            }

            // FAI Final: chỉ giữ lại dims có trạng thái Fail để re-inspect
            if (IsFinalMode)
            {
                var failDims = Dimensions.Where(d => d.State == MeasureState.Fail).ToList();
                Dimensions.Clear();
                foreach (var d in failDims) Dimensions.Add(d);
            }

            RefreshProgress();

            if (!Dimensions.Any())
                ErrorMessage = IsFinalMode
                    ? "Không có kích thước nào ở trạng thái FAIL để re-inspect."
                    : "OP này chưa có kích thước nào được định nghĩa.";
            else
                SelectedDimension = Dimensions.FirstOrDefault(d =>
                    IsFinalMode ? d.State == MeasureState.Fail : !d.IsMeasured);
        }
        catch (Exception ex) { ErrorMessage = $"Lỗi: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    // ── Numeric input confirm ──────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private async Task ConfirmAsync()
    {
        if (SelectedDimension is null) return;

        if (!decimal.TryParse(InputValue.Replace(',', '.'),
                NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
        {
            ErrorMessage = "Giá trị không hợp lệ.";
            return;
        }

        await SaveAsync(value, null);
    }

    private bool CanConfirm() =>
        SelectedDimension is { IsTextType: false } &&
        !string.IsNullOrWhiteSpace(InputValue) &&
        !IsInputLocked;

    // ── Text type: PASS / FAIL buttons ────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanSetPass))]
    private async Task SetPassAsync() => await SaveAsync(null, true);
    private bool CanSetPass() => !IsInputLocked;

    [RelayCommand(CanExecute = nameof(CanSetFail))]
    private async Task SetFailAsync() => await SaveAsync(null, false);
    private bool CanSetFail() => !IsInputLocked;

    // ── Core save logic ───────────────────────────────────────────────────

    private async Task SaveAsync(decimal? value, bool? manualResult)
    {
        if (SelectedDimension is null) return;
        IsBusy = true; ClearError();
        try
        {
            var req = new
            {
                DimensionId  = SelectedDimension.Id,
                ProductId    = Product!.ProductId,
                Value        = value,
                ManualResult = manualResult,
                IsFinal      = IsFinalMode,
                Note         = (string?)null
            };

            var resp = await _api.PostAsync<object, MeasureResultResponse>("/api/v1/fai/measure", req);
            if (resp?.Data is null) { ErrorMessage = "Không thể lưu kết quả đo."; return; }

            var current = SelectedDimension;
            current.State         = resp.Data.Result == "Pass" ? MeasureState.Pass : MeasureState.Fail;
            current.MeasuredValue = value;

            if (current.State == MeasureState.Fail && OnDimensionFail is not null)
            {
                var args = new NcrTriggerArgs(
                    JobId:          Job!.Id,
                    ProductId:      Product!.ProductId,
                    PartOpId:       Op!.Id,
                    BalloonNumber:  current.BalloonNumber,
                    MeasuredValue:  value,
                    MinValue:       current.MinValue,
                    MaxValue:       current.MaxValue,
                    MachineCode:    _settings.MachineCode);
                OnDimensionFail.Invoke(args);
            }

            InputValue        = "";
            SelectedDimension = IsFinalMode
                ? Dimensions.FirstOrDefault(d => d.State == MeasureState.Fail)
                : Dimensions.FirstOrDefault(d => !d.IsMeasured);
            RefreshProgress();
        }
        catch (Exception ex) { ErrorMessage = $"Lỗi: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void GoBack() => OnBack?.Invoke();

    private void RefreshProgress()
    {
        OnPropertyChanged(nameof(MeasuredCount));
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(Progress));
    }
}
