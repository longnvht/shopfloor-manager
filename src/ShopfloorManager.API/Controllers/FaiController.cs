using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopfloorManager.API.Common;
using ShopfloorManager.Application.Production;
using ShopfloorManager.Domain.Enums;

namespace ShopfloorManager.API.Controllers;

[ApiController]
[Route("api/v1/fai")]
[Authorize]
public class FaiController(IMediator mediator) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    /// <summary>FAI Sheet: ma trận (Serials × Dimensions) cho một OP của một Job. partOpId null = Tất cả OP.</summary>
    [HttpGet]
    public async Task<IActionResult> GetFaiSheet([FromQuery] int jobId, [FromQuery] int? partOpId)
    {
        var result = await mediator.Send(new GetFaiSheetQuery(jobId, partOpId));
        return result.IsSuccess
            ? Ok(ApiResponse<FaiSheetDto>.Ok(result.Value))
            : BadRequest(ApiResponse<FaiSheetDto>.Fail(result.Errors));
    }

    /// <summary>
    /// Nhập giá trị đo — tạo record mới mỗi lần (giữ lịch sử).
    /// Số: auto Pass/Fail vs MaxValue/MinValue.
    /// Text: operator truyền ManualResult=true(Pass)/false(Fail).
    /// MeasureStage: 0=InprocessFAI (default), 1=QCInline, 2=QCFinal.
    /// </summary>
    [HttpPost("measure")]
    [Authorize(Roles = "Administrator,Manager,Engineer,QC Inspector,Operator,Lead Engineer")]
    public async Task<IActionResult> SaveMeasure([FromBody] SaveMeasureRequest req)
    {
        var result = await mediator.Send(new SaveMeasureCommand(
            req.DimensionId, req.ProductId, req.Value,
            req.ManualResult, req.IsFinal, req.FinalOpId, req.Note, UserId,
            req.MeasureStage));
        return result.IsSuccess
            ? Ok(ApiResponse<MeasureValueDto>.Ok(result.Value))
            : BadRequest(ApiResponse<MeasureValueDto>.Fail(result.Errors));
    }

    /// <summary>Xem toàn bộ dimension của 1 Product (Serial) xuyên suốt mọi OP trong Job — review/export.</summary>
    [HttpGet("product/{productId:int}")]
    public async Task<IActionResult> GetProductMeasureSheet(int productId)
    {
        var result = await mediator.Send(new GetProductMeasureSheetQuery(productId));
        return result.IsSuccess
            ? Ok(ApiResponse<ProductMeasureSheetDto>.Ok(result.Value))
            : BadRequest(ApiResponse<ProductMeasureSheetDto>.Fail(result.Errors));
    }

    /// <summary>Tiến độ QC Final cho một Product (số dimension đã đo ở stage QCFinal).</summary>
    [HttpGet("/api/v1/products/{productId:int}/qcfinal-progress")]
    public async Task<IActionResult> GetQcFinalProgress(int productId)
    {
        var result = await mediator.Send(new GetQcFinalProgressQuery(productId));
        return result.IsSuccess
            ? Ok(ApiResponse<QcFinalProgressDto>.Ok(result.Value))
            : BadRequest(ApiResponse<QcFinalProgressDto>.Fail(result.Errors));
    }

    /// <summary>Export FAI sheet ra Excel — review/report, stage null = Tất cả.</summary>
    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportExcel([FromQuery] int jobId, [FromQuery] int? partOpId, [FromQuery] int? stage)
    {
        var result = await mediator.Send(new GetFaiSheetQuery(jobId, partOpId));
        if (!result.IsSuccess) return BadRequest(ApiResponse<FaiSheetDto>.Fail(result.Errors));
        var bytes = FaiExportBuilder.BuildExcel(result.Value, stage);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"FAI_OP{result.Value.OpNumber}.xlsx");
    }

    /// <summary>Export FAI sheet ra PDF — review/report, stage null = Tất cả.</summary>
    [HttpGet("export/pdf")]
    public async Task<IActionResult> ExportPdf([FromQuery] int jobId, [FromQuery] int? partOpId, [FromQuery] int? stage)
    {
        var result = await mediator.Send(new GetFaiSheetQuery(jobId, partOpId));
        if (!result.IsSuccess) return BadRequest(ApiResponse<FaiSheetDto>.Fail(result.Errors));
        var bytes = FaiExportBuilder.BuildPdf(result.Value, stage);
        return File(bytes, "application/pdf", $"FAI_OP{result.Value.OpNumber}.pdf");
    }

    // ── QC Inline Rate config ────────────────────────────────

    /// <summary>List toàn bộ mức kiểm QC Inline — phục vụ trang Master Data (Web).</summary>
    [HttpGet("/api/v1/qc-inline-rates")]
    public async Task<IActionResult> GetQcInlineRates()
    {
        var result = await mediator.Send(new GetQcInlineRatesQuery());
        return Ok(ApiResponse<List<QcInlineRateDto>>.Ok(result.Value));
    }

    /// <summary>Mức kiểm hiệu lực cho 1 Job/OP — Desktop dùng để hiển thị banner ở màn QC Inline.</summary>
    [HttpGet("/api/v1/fai/qc-inline-rate")]
    public async Task<IActionResult> GetEffectiveQcInlineRate([FromQuery] int jobId, [FromQuery] int? partOpId)
    {
        var result = await mediator.Send(new GetEffectiveQcInlineRateQuery(jobId, partOpId));
        return result.IsSuccess
            ? Ok(ApiResponse<decimal>.Ok(result.Value))
            : Ok(ApiResponse<decimal>.Ok(0));
    }

    [HttpPost("/api/v1/qc-inline-rates")]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<IActionResult> CreateQcInlineRate([FromBody] CreateQcInlineRateCommand command)
    {
        var result = await mediator.Send(command);
        return result.IsSuccess
            ? StatusCode(201, ApiResponse<QcInlineRateDto>.Ok(result.Value))
            : BadRequest(ApiResponse<QcInlineRateDto>.Fail(result.Errors));
    }

    [HttpPut("/api/v1/qc-inline-rates/{id:int}")]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<IActionResult> UpdateQcInlineRate(int id, [FromBody] UpdateQcInlineRateCommand command)
    {
        if (id != command.Id) return BadRequest(ApiResponse<QcInlineRateDto>.Fail("ID không khớp."));
        var result = await mediator.Send(command);
        return result.IsSuccess
            ? Ok(ApiResponse<QcInlineRateDto>.Ok(result.Value))
            : BadRequest(ApiResponse<QcInlineRateDto>.Fail(result.Errors));
    }
}

public record SaveMeasureRequest(
    long DimensionId, int ProductId,
    decimal? Value,
    bool? ManualResult,    // Dùng cho text dimension
    bool IsFinal = false,  // true khi re-inspect sau rework (FAI Final)
    int? FinalOpId = null,
    string? Note = null,
    MeasureStage MeasureStage = MeasureStage.InprocessFAI);
