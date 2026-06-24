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

    /// <summary>Basic = Operator (InprocessFAI), đo theo thứ tự, khóa sau khi đo.
    /// Final = QC Final (QCFinal) — kiểm tra độc lập 100% dimension, KHÔNG đọc/tham chiếu kết quả
    /// InprocessFAI hay QCInline ("blind inspection"), khóa sau khi đo, giống hành vi của Basic.
    /// QcInline = QC Inspector kiểm ngẫu nhiên (QCInline), không bắt buộc đo hết, không tự chọn dimension tiếp theo.</summary>
    public FaiMode Mode { get; set; } = FaiMode.Basic;

    /// <summary>Binding read-only cho XAML — giữ tương thích với trigger màu title bar hiện có.</summary>
    public bool IsFinalMode => Mode == FaiMode.Final;
    public bool IsQcInlineMode => Mode == FaiMode.QcInline;

    public string PageTitle => Mode switch
    {
        FaiMode.Final    => "NHẬP KẾT QUẢ ĐO (FAI FINAL)",
        FaiMode.QcInline => "NHẬP KẾT QUẢ ĐO (QC INLINE)",
        _                => "NHẬP KẾT QUẢ ĐO (FAI)",
    };

    [ObservableProperty]
    private string? _rateInfoText;

    public string TitleContext => Product is null ? "" :
        $"{Job?.JobNumber}  ·  OP {Op?.OpNumber}  ·  S/N: {Product.SerialNumber}";

    public ObservableCollection<DimensionCardVm> Dimensions { get; } = [];

    public ObservableCollection<MesGageData> AvailableGages { get; } = [];
    public ObservableCollection<MesGageData> FilteredGages  { get; } = [];

    [ObservableProperty]
    private MesGageData? _selectedGage;

    /// <summary>true = đang hiện ô tìm + danh sách thẻ; false = đã chọn, chỉ hiện chip tóm tắt.</summary>
    [ObservableProperty]
    private bool _isGageSearchOpen = true;

    [ObservableProperty]
    private string _gageSearchText = "";

    /// <summary>Thẻ đang được click chọn (chưa xác nhận) — click chỉ highlight để không xung đột với
    /// kéo-thả cuộn danh sách; phải double-click hoặc bấm "Chọn" mới chốt vào SelectedGage.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmHighlightedGageCommand))]
    private MesGageData? _highlightedGage;

    partial void OnGageSearchTextChanged(string value) => RefreshFilteredGages();

    private void RefreshFilteredGages()
    {
        FilteredGages.Clear();
        var matches = string.IsNullOrWhiteSpace(GageSearchText)
            ? AvailableGages
            : AvailableGages.Where(g =>
                g.GageNo.Contains(GageSearchText, StringComparison.OrdinalIgnoreCase) ||
                g.Description.Contains(GageSearchText, StringComparison.OrdinalIgnoreCase));
        foreach (var g in matches) FilteredGages.Add(g);
    }

    [RelayCommand]
    private void SelectGage(MesGageData gage)
    {
        SelectedGage = gage;
        HighlightedGage = null;
        IsGageSearchOpen = false;
        GageSearchText = "";
    }

    [RelayCommand(CanExecute = nameof(CanConfirmHighlightedGage))]
    private void ConfirmHighlightedGage() => SelectGage(HighlightedGage!);
    private bool CanConfirmHighlightedGage() => HighlightedGage is not null;

    [RelayCommand]
    private void ChangeGage()
    {
        IsGageSearchOpen = true;
        GageSearchText = "";
        HighlightedGage = null;
        RefreshFilteredGages();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPlaceholder))]
    [NotifyPropertyChangedFor(nameof(ShowNumericInput))]
    [NotifyPropertyChangedFor(nameof(ShowTextInput))]
    [NotifyPropertyChangedFor(nameof(IsInputLocked))]
    [NotifyPropertyChangedFor(nameof(IsInputEnabled))]
    [NotifyPropertyChangedFor(nameof(ShowGageSelection))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    [NotifyCanExecuteChangedFor(nameof(SetPassCommand))]
    [NotifyCanExecuteChangedFor(nameof(SetFailCommand))]
    private DimensionCardVm? _selectedDimension;

    /// <summary>Category VIS = kiểm bằng mắt (visual inspection) — không cần dụng cụ đo.</summary>
    public bool ShowGageSelection => SelectedDimension is not null && SelectedDimension.GageTypeCode != "VIS";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string _inputValue = "";

    public int    MeasuredCount => Dimensions.Count(d => d.IsMeasured);
    public int    TotalCount    => Dimensions.Count;
    public double Progress      => TotalCount > 0 ? (double)MeasuredCount / TotalCount : 0;

    public bool ShowPlaceholder  => SelectedDimension is null;
    public bool ShowNumericInput => SelectedDimension is { IsTextType: false };
    public bool ShowTextInput    => SelectedDimension is { IsTextType: true };
    // Một rule chung cho cả 3 mode: khóa sau khi đã đo (bất kể Pass/Fail) — mỗi mode chỉ đọc/ghi
    // đúng MeasureStage của riêng nó (xem ToServerStage), không có ngoại lệ.
    public bool IsInputLocked    => SelectedDimension?.IsMeasured == true;
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

        _ = LoadGagesAsync(value);
    }

    /// <summary>Danh sách gage hợp lệ phù hợp dimension đang chọn — xem 08_gage_management.md §6 (MES).
    /// Đổi dimension luôn yêu cầu chọn lại gage (KHÔNG carry-over từ dimension trước) — trừ khi job hiện
    /// tại đã có gage dùng cho đúng balloon này ở serial khác (xem WorkContext.LastGageIdByBalloon).</summary>
    private async Task LoadGagesAsync(DimensionCardVm? dim)
    {
        if (dim?.GageTypeCode == "VIS")
        {
            AvailableGages.Clear();
            FilteredGages.Clear();
            SelectedGage = null;
            HighlightedGage = null;
            return;
        }

        try
        {
            // GageTypeId chính xác hơn CategoryCode (1 category có thể gồm nhiều GageType) — ưu tiên khi có.
            var url = dim?.GageTypeId is int gageTypeId
                ? $"/api/v1/mes/gages?gageTypeId={gageTypeId}"
                : string.IsNullOrWhiteSpace(dim?.CategoryCode)
                    ? "/api/v1/mes/gages"
                    : $"/api/v1/mes/gages?categoryCode={dim!.CategoryCode}";
            var resp = await _api.GetAsync<List<MesGageData>>(url);

            AvailableGages.Clear();
            foreach (var gage in resp?.Data ?? [])
                AvailableGages.Add(gage);

            MesGageData? remembered = null;
            if (dim is not null && _work.LastGageIdByBalloon.TryGetValue(GageMemoryKey(dim.BalloonNumber), out var lastGageId))
                remembered = AvailableGages.FirstOrDefault(g => g.Id == lastGageId);

            SelectedGage = remembered;
            HighlightedGage = null;
            IsGageSearchOpen = remembered is null;
            GageSearchText = "";
            RefreshFilteredGages();
        }
        catch { /* Gage selection là tùy chọn — lỗi tải danh sách không chặn nhập đo */ }
    }

    /// <summary>Key cho WorkContext.LastGageIdByBalloon — gồm PartOpId vì BalloonNumber chỉ UNIQUE trong 1 PartOp
    /// (cùng số bóng có thể là dimension khác nhau ở OP khác).</summary>
    private string GageMemoryKey(string balloonNumber) => $"{Op!.Id}:{balloonNumber}";

    /// <summary>Khớp thủ công với ShopfloorManager.Domain.Enums.MeasureStage trên server (int): 0/1/2.</summary>
    private static int ToServerStage(FaiMode mode) => mode switch
    {
        FaiMode.Final    => 2, // QCFinal
        FaiMode.QcInline => 1, // QCInline
        _                => 0, // InprocessFAI
    };

    private async Task LoadRateInfoAsync()
    {
        try
        {
            var resp = await _api.GetAsync<decimal>($"/api/v1/fai/qc-inline-rate?jobId={Job!.Id}&partOpId={Op!.Id}");
            RateInfoText = resp?.Data is decimal rate ? $"Mức kiểm đề xuất: {rate:0.#}%" : null;
        }
        catch { RateInfoText = null; }
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

            var stageKey = ToServerStage(Mode);
            foreach (var dim in resp.Data.Dimensions ?? [])
            {
                var cell = row?.Cells?.FirstOrDefault(c => c.BalloonNumber == dim.BalloonNumber);
                // Đọc giá trị riêng của stage hiện tại — KHÔNG dùng cell.Result/Value (đó là "mới nhất
                // qua mọi stage", có thể lẫn dữ liệu của stage khác cho cùng dimension/product). QC Final
                // là "blind inspection" — không đọc/tham chiếu InprocessFAI hay QCInline.
                var stageCell = cell?.ByStage?.GetValueOrDefault(stageKey);
                var state = stageCell?.Result switch
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
                    CategoryCode  = dim.CategoryCode,
                    GageTypeId    = dim.GageTypeId,
                    GageTypeCode  = dim.GageTypeCode,
                    State         = state,
                    MeasuredValue = stageCell?.Value
                });
            }

            RefreshProgress();

            if (!Dimensions.Any())
                ErrorMessage = "OP này chưa có kích thước nào được định nghĩa.";
            else
                SelectedDimension = Mode == FaiMode.QcInline
                    ? null
                    : Dimensions.FirstOrDefault(d => !d.IsMeasured);

            if (Mode == FaiMode.QcInline)
                _ = LoadRateInfoAsync();
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
                IsFinal      = Mode == FaiMode.Final,
                Note         = (string?)null,
                MeasureStage = ToServerStage(Mode),
                GageId       = SelectedGage?.Id
            };

            var resp = await _api.PostAsync<object, MeasureResultResponse>("/api/v1/fai/measure", req);
            if (resp?.Data is null) { ErrorMessage = "Không thể lưu kết quả đo."; return; }

            var current = SelectedDimension;
            current.State         = resp.Data.Result == "Pass" ? MeasureState.Pass : MeasureState.Fail;
            current.MeasuredValue = value;

            if (SelectedGage is not null)
                _work.LastGageIdByBalloon[GageMemoryKey(current.BalloonNumber)] = SelectedGage.Id;

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
            SelectedDimension = Mode == FaiMode.QcInline
                ? null  // QC tự chọn balloon tiếp theo muốn kiểm, không auto-advance
                : Dimensions.FirstOrDefault(d => !d.IsMeasured);  // Basic & Final: auto-advance
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
