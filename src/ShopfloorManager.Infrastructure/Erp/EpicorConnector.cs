using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Application.Production;

namespace ShopfloorManager.Infrastructure.Erp;

/// <summary>
/// Kết nối Epicor ERP qua REST API v1 (OData v4).
/// URL pattern: {BaseUrl}/api/v1/Erp.BO.{Service}Svc/{EntitySet}
/// Auth: Basic (username:password)
/// Ref: Epicor Developer Reference — REST API
/// </summary>
public class EpicorConnector : IErpConnector
{
    private readonly HttpClient _http;
    private readonly string _company;

    public EpicorConnector(string baseUrl, string? company, string? username, string? password)
    {
        _company = company ?? "EPIC06";
        _http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") };
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
        {
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encoded);
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            // Epicor: GET /api/v1/Erp.BO.CompanySvc/Companies?$top=1 là endpoint nhẹ nhất để test
            var res = await _http.GetAsync("api/v1/Erp.BO.CompanySvc/Companies?$top=1", ct);
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ErpPreviewResult> FetchPreviewAsync(ErpImportFilter filter, CancellationToken ct = default)
    {
        var warnings = new List<string>();

        // 1. Lấy JobHead (header lệnh SX)
        var jobHeads = await FetchJobHeadsAsync(filter, warnings, ct);

        if (jobHeads.Count == 0)
            return new ErpPreviewResult([], 0, warnings);

        var jobNums = jobHeads.Select(j => j.JobNum).Distinct().ToList();

        // 2. Lấy JobOper (các công đoạn) theo danh sách JobNum
        var jobOpers = await FetchJobOpersAsync(jobNums, warnings, ct);

        // 3. Build ImportJobBatchRow list
        var rows = new List<ImportJobBatchRow>();
        foreach (var head in jobHeads)
        {
            var opers = jobOpers.Where(o => o.JobNum == head.JobNum).OrderBy(o => o.OprSeq).ToList();
            if (opers.Count == 0)
            {
                // Job không có OP — tạo 1 dòng placeholder để job vẫn được import
                rows.Add(new ImportJobBatchRow(
                    head.PartNum, head.PartDescription, head.RevisionNum,
                    head.JobNum, head.PONum, head.POLine?.ToString(),
                    (int?)head.OrderQty, head.DueDate.HasValue ? DateOnly.FromDateTime(head.DueDate.Value) : null,
                    "10", null, "N/A", null, null));
            }
            else
            {
                foreach (var op in opers)
                {
                    // Epicor lưu thời gian setup/prod theo giờ → chuyển sang phút
                    decimal? setupMin = op.EstSetHours.HasValue ? (decimal?)Math.Round(op.EstSetHours.Value * 60, 1) : null;
                    decimal? prodMin  = op.ProdStandard.HasValue ? (decimal?)Math.Round(op.ProdStandard.Value * 60, 1) : null;

                    rows.Add(new ImportJobBatchRow(
                        head.PartNum, head.PartDescription, head.RevisionNum,
                        head.JobNum, head.PONum, head.POLine?.ToString(),
                        (int?)head.OrderQty, head.DueDate.HasValue ? DateOnly.FromDateTime(head.DueDate.Value) : null,
                        op.OprSeq.ToString(), op.OpCode, op.CommentText,
                        setupMin, prodMin));
                }
            }
        }

        return new ErpPreviewResult(rows, jobHeads.Count, warnings);
    }

    // ── Private fetch helpers ─────────────────────────────────────────────────

    private async Task<List<EpicorJobHead>> FetchJobHeadsAsync(ErpImportFilter filter, List<string> warnings, CancellationToken ct)
    {
        var filters = new List<string> { $"Company eq '{_company}'" };
        if (filter.DateFrom.HasValue) filters.Add($"DueDate ge {filter.DateFrom.Value:yyyy-MM-dd}T00:00:00Z");
        if (filter.DateTo.HasValue)   filters.Add($"DueDate le {filter.DateTo.Value:yyyy-MM-dd}T23:59:59Z");
        if (!string.IsNullOrEmpty(filter.PoNumber)) filters.Add($"PONum eq '{filter.PoNumber}'");

        var select  = "$select=JobNum,PartNum,PartDescription,RevisionNum,OrderQty,DueDate,PONum,POLine";
        var filterQ = "$filter=" + Uri.EscapeDataString(string.Join(" and ", filters));
        var topSkip = $"$top={filter.PageSize}&$skip={filter.Page * filter.PageSize}&$count=true";
        var url     = $"api/v1/Erp.BO.JobEntrySvc/JobHeads?{select}&{filterQ}&{topSkip}";

        try
        {
            var json = await _http.GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(json);
            var values = doc.RootElement.GetProperty("value");
            return JsonSerializer.Deserialize<List<EpicorJobHead>>(values.GetRawText(), JsonOpts) ?? [];
        }
        catch (Exception ex)
        {
            warnings.Add($"Lỗi lấy JobHeads từ Epicor: {ex.Message}");
            return [];
        }
    }

    private async Task<List<EpicorJobOper>> FetchJobOpersAsync(List<string> jobNums, List<string> warnings, CancellationToken ct)
    {
        if (jobNums.Count == 0) return [];

        // OData $filter: JobNum in ('J1','J2',...) hoặc dùng multiple eq với or
        var jobFilter = string.Join(" or ", jobNums.Select(j => $"JobNum eq '{j}'"));
        var select    = "$select=JobNum,OprSeq,OpCode,CommentText,EstSetHours,ProdStandard";
        var url       = $"api/v1/Erp.BO.JobEntrySvc/JobOprs?{select}&$filter={Uri.EscapeDataString($"Company eq '{_company}' and ({jobFilter})")}&$top=1000";

        try
        {
            var json = await _http.GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(json);
            var values = doc.RootElement.GetProperty("value");
            return JsonSerializer.Deserialize<List<EpicorJobOper>>(values.GetRawText(), JsonOpts) ?? [];
        }
        catch (Exception ex)
        {
            warnings.Add($"Lỗi lấy JobOprs từ Epicor: {ex.Message}");
            return [];
        }
    }

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    // ── Epicor response models ────────────────────────────────────────────────

    private class EpicorJobHead
    {
        public string JobNum { get; set; } = "";
        public string PartNum { get; set; } = "";
        public string PartDescription { get; set; } = "";
        public string RevisionNum { get; set; } = "";
        public double? OrderQty { get; set; }
        public DateTime? DueDate { get; set; }
        public string? PONum { get; set; }
        public int? POLine { get; set; }
    }

    private class EpicorJobOper
    {
        public string JobNum { get; set; } = "";
        public int OprSeq { get; set; }
        public string? OpCode { get; set; }
        public string? CommentText { get; set; }
        public double? EstSetHours { get; set; }
        public double? ProdStandard { get; set; }
    }
}
