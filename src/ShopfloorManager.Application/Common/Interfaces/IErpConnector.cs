using ShopfloorManager.Application.Production;

namespace ShopfloorManager.Application.Common.Interfaces;

public record ErpImportFilter(
    DateOnly? DateFrom,
    DateOnly? DateTo,
    string? PoNumber,
    int Page = 0,
    int PageSize = 100);

public record ErpPreviewResult(
    List<ImportJobBatchRow> Rows,
    int TotalCount,
    List<string> Warnings);

public interface IErpConnector
{
    Task<bool> TestConnectionAsync(CancellationToken ct = default);
    Task<ErpPreviewResult> FetchPreviewAsync(ErpImportFilter filter, CancellationToken ct = default);
}

public interface IErpConnectorFactory
{
    /// <summary>Tạo connector phù hợp dựa trên ErpType.</summary>
    IErpConnector Create(string erpType, string baseUrl, string? company, string? username, string? password);
}
