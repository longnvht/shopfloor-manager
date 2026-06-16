using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Application.Production;

namespace ShopfloorManager.Infrastructure.Erp;

/// <summary>
/// Connector giả — trả dữ liệu cứng giống format Epicor để test toàn bộ UI + import flow
/// mà không cần server Epicor thật.
/// </summary>
public class MockErpConnector : IErpConnector
{
    private static readonly List<ImportJobBatchRow> _allRows =
    [
        // HOUSING-AL6061 / Rev A / J2026-M01 — 3 OP
        new("HOUSING-AL6061", "Aluminum Housing 6061-T6", "A",
            "J2026-M01", "PO-2026-001", "1", 5, new DateOnly(2026, 7, 15),
            "10", "CNC", "Rough Turning", 30, 45),
        new("HOUSING-AL6061", "Aluminum Housing 6061-T6", "A",
            "J2026-M01", "PO-2026-001", "1", 5, new DateOnly(2026, 7, 15),
            "20", "MILL", "Face Milling + Bore", 45, 90),
        new("HOUSING-AL6061", "Aluminum Housing 6061-T6", "A",
            "J2026-M01", "PO-2026-001", "1", 5, new DateOnly(2026, 7, 15),
            "30", "INSP", "Final Inspection", 15, 30),

        // BRACKET-ST304 / Rev B / J2026-M02 — 3 OP
        new("BRACKET-ST304", "Stainless Bracket 304", "B",
            "J2026-M02", "PO-2026-002", "1", 10, new DateOnly(2026, 7, 20),
            "10", "CNC", "Profile Milling", 60, 120),
        new("BRACKET-ST304", "Stainless Bracket 304", "B",
            "J2026-M02", "PO-2026-002", "1", 10, new DateOnly(2026, 7, 20),
            "20", "GRIND", "Surface Grinding", 30, 60),
        new("BRACKET-ST304", "Stainless Bracket 304", "B",
            "J2026-M02", "PO-2026-002", "1", 10, new DateOnly(2026, 7, 20),
            "30", "INSP", "CMM Inspection", 20, 20),

        // SHAFT-16H7 / Rev A / J2026-M03 — 4 OP
        new("SHAFT-16H7", "16mm H7 Precision Shaft", "A",
            "J2026-M03", "PO-2026-003", "1", 20, new DateOnly(2026, 7, 31),
            "10", "TURN", "CNC Turning OD", 45, 90),
        new("SHAFT-16H7", "16mm H7 Precision Shaft", "A",
            "J2026-M03", "PO-2026-003", "1", 20, new DateOnly(2026, 7, 31),
            "20", "GRIND", "Cylindrical Grinding H7", 60, 180),
        new("SHAFT-16H7", "16mm H7 Precision Shaft", "A",
            "J2026-M03", "PO-2026-003", "1", 20, new DateOnly(2026, 7, 31),
            "30", "THD", "Thread Cutting M16", 20, 30),
        new("SHAFT-16H7", "16mm H7 Precision Shaft", "A",
            "J2026-M03", "PO-2026-003", "1", 20, new DateOnly(2026, 7, 31),
            "40", "INSP", "Final Dimensional Check", 15, 20),
    ];

    public Task<bool> TestConnectionAsync(CancellationToken ct = default) =>
        Task.FromResult(true);

    public Task<ErpPreviewResult> FetchPreviewAsync(ErpImportFilter filter, CancellationToken ct = default)
    {
        var rows = _allRows.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter.PoNumber))
            rows = rows.Where(r => r.PoNumber?.Contains(filter.PoNumber, StringComparison.OrdinalIgnoreCase) == true);

        var list = rows.ToList();
        var warnings = new List<string>
        {
            "MockErpConnector: dữ liệu giả (không kết nối ERP thật).",
            "OpCode 'THD' và 'TURN' có thể chưa tồn tại trong OpTypes của hệ thống — sẽ import với OpTypeId=null.",
        };

        return Task.FromResult(new ErpPreviewResult(list, list.Count, warnings));
    }
}
