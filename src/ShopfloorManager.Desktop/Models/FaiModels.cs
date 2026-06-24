using CommunityToolkit.Mvvm.ComponentModel;

namespace ShopfloorManager.Desktop.Models;

// ── API response DTOs ──────────────────────────────────────────────────────

public record FaiSheetResponse(
    int JobId, int PartOpId, string OpNumber,
    List<DimensionData> Dimensions,
    List<FaiRowData> Rows);

public record DimensionData(
    long Id, int PartOpId,
    string BalloonNumber, decimal? BalloonSort, string? Code, string? Description,
    decimal? NominalValue, decimal? TolerancePlus, decimal? ToleranceMinus,
    decimal? MaxValue, decimal? MinValue, string? Unit,
    bool IsTextType, string? NominalText,
    string? CategoryCode, bool IsCritical, bool IsFinal, int SortOrder,
    int? GageTypeId = null, string? GageTypeCode = null);

public record FaiRowData(
    string SerialNumber, int ProductId,
    List<FaiCellData> Cells, bool AllPass);

public record FaiStageCellData(decimal? Value, string? Result);

public record FaiCellData(
    long? MeasureValueId, string BalloonNumber,
    decimal? Value, string? Result,
    Dictionary<int, FaiStageCellData>? ByStage = null);

/// <summary>
/// 3 chế độ FAI trên Desktop — KHÔNG tham chiếu ShopfloorManager.Domain.Enums.MeasureStage
/// (Desktop không có project reference tới Domain). Giá trị int khớp thủ công với
/// ShopfloorManager.Domain.Enums.MeasureStage: InprocessFAI=0, QCInline=1, QCFinal=2.
/// </summary>
public enum FaiMode { Basic, Final, QcInline }

public record MeasureResultResponse(
    long Id, string BalloonNumber, decimal? Value, string Result);

/// <summary>Gage hợp lệ, chưa bị mượn — danh sách trả về từ GET /api/v1/mes/gages.</summary>
public record MesGageData(int Id, string GageNo, string Description, string? Unit, string? CategoryCode)
{
    public string Display => $"{GageNo} — {Description}";
}

// ── Card ViewModel ─────────────────────────────────────────────────────────

public enum MeasureState { Unmeasured, Pass, Fail }

public partial class DimensionCardVm : ObservableObject
{
    public long Id { get; init; }
    public string BalloonNumber { get; init; } = "";
    public decimal? NominalValue { get; init; }
    public decimal? TolerancePlus { get; init; }
    public decimal? ToleranceMinus { get; init; }
    public decimal? MaxValue { get; init; }
    public decimal? MinValue { get; init; }
    public string Unit { get; init; } = "";
    public bool IsTextType { get; init; }
    public string? NominalText { get; init; }
    public bool IsFinal { get; init; }
    public bool IsCritical { get; init; }
    public string? CategoryCode { get; init; }
    /// <summary>Loại dụng cụ đo cụ thể (MIC/CAL/BOR...) — chi tiết hơn CategoryCode, ưu tiên dùng để
    /// filter gage khi nhập đo nếu có (xem FaiViewModel.LoadGagesAsync).</summary>
    public int? GageTypeId { get; init; }
    public string? GageTypeCode { get; init; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMeasured))]
    [NotifyPropertyChangedFor(nameof(StateLabel))]
    private MeasureState _state = MeasureState.Unmeasured;

    [ObservableProperty]
    private decimal? _measuredValue;

    public bool IsMeasured => State is MeasureState.Pass or MeasureState.Fail;

    /// <summary>Hiện GageTypeCode (chi tiết hơn, ví dụ "MIC") nếu có, fallback về CategoryCode (ví dụ "LIN").</summary>
    public string? GageBadgeText => GageTypeCode ?? CategoryCode;

    public string NominalDisplay => IsTextType
        ? (NominalText ?? "TEXT")
        : (NominalValue?.ToString("F4") ?? "—");

    public string TolDisplay => IsTextType ? ""
        : $"+{TolerancePlus:F4} / -{ToleranceMinus:F4}";

    public string StateLabel => State switch
    {
        MeasureState.Pass => "PASS",
        MeasureState.Fail => "FAIL",
        _ => IsTextType ? "TEXT" : "Chưa đo"
    };
}
