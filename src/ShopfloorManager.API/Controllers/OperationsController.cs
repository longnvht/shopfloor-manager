using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopfloorManager.API.Common;
using ShopfloorManager.Application.Production;

namespace ShopfloorManager.API.Controllers;

[ApiController]
[Route("api/v1/operations")]
[Authorize]
public class OperationsController(IMediator mediator) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    /// <summary>Danh sách PartOps theo RoutingRevId (template).</summary>
    [HttpGet]
    public async Task<IActionResult> GetOps([FromQuery] int? routingRevId)
    {
        var result = await mediator.Send(new GetRoutingRevOpsQuery(routingRevId ?? 0));
        return Ok(ApiResponse<List<PartOpDto>>.Ok(result.Value));
    }

    /// <summary>Tạo PartOp (template cho RoutingRev hoặc ForJobOnly).</summary>
    [HttpPost]
    [Authorize(Roles = "Administrator,Manager,Engineer")]
    public async Task<IActionResult> CreateOp([FromBody] CreateOpRequest req)
    {
        var result = await mediator.Send(new CreatePartOpCommand(
            req.RoutingRevId, req.JobId, req.OpNumber, req.OpTypeId,
            req.Description, req.Note, req.SetupTime, req.ProdTime, UserId));
        return result.IsSuccess
            ? StatusCode(201, ApiResponse<PartOpDto>.Ok(result.Value))
            : BadRequest(ApiResponse<PartOpDto>.Fail(result.Errors));
    }

    /// <summary>Danh sách dimensions của một OP.</summary>
    [HttpGet("{opId:int}/dimensions")]
    public async Task<IActionResult> GetDimensions(int opId, [FromQuery] int jobId)
    {
        var result = await mediator.Send(new GetFaiSheetQuery(jobId, opId));
        if (result.IsFailed) return BadRequest(ApiResponse<object>.Fail(result.Errors));
        return Ok(ApiResponse<IReadOnlyList<DimensionDto>>.Ok(result.Value.Dimensions));
    }

    /// <summary>Danh sách dimensions định nghĩa của một OP — không cần JobId (dùng cho trang Part &amp; Routing).</summary>
    [HttpGet("{opId:int}/dimensions/definitions")]
    public async Task<IActionResult> GetDimensionDefinitions(int opId)
    {
        var result = await mediator.Send(new GetOpDimensionsQuery(opId));
        return result.IsSuccess
            ? Ok(ApiResponse<IReadOnlyList<DimensionDto>>.Ok(result.Value))
            : BadRequest(ApiResponse<IReadOnlyList<DimensionDto>>.Fail(result.Errors));
    }

    /// <summary>Tổng hợp toàn bộ Dimension của các PartOp template thuộc một RoutingRev — dùng cho trang "Dimension Sheet".</summary>
    [HttpGet("/api/v1/routing-revs/{routingRevId:int}/dimensions")]
    public async Task<IActionResult> GetRoutingRevDimensions(int routingRevId)
    {
        var result = await mediator.Send(new GetDimensionsByRoutingRevQuery(routingRevId));
        return result.IsSuccess
            ? Ok(ApiResponse<List<RoutingRevDimensionDto>>.Ok(result.Value))
            : BadRequest(ApiResponse<List<RoutingRevDimensionDto>>.Fail(result.Errors));
    }

    /// <summary>Sửa Nominal/Tolerance của một Dimension (inline edit từ Dimension Sheet).</summary>
    [HttpPut("/api/v1/dimensions/{id:long}")]
    [Authorize(Roles = "Administrator,Manager,Engineer,QC Inspector")]
    public async Task<IActionResult> UpdateDimension(long id, [FromBody] UpdateDimensionRequest req)
    {
        var result = await mediator.Send(new UpdateDimensionCommand(
            id, req.NominalValue, req.TolerancePlus, req.ToleranceMinus, UserId));
        return result.IsSuccess
            ? Ok(ApiResponse<DimensionDto>.Ok(result.Value))
            : BadRequest(ApiResponse<DimensionDto>.Fail(result.Errors));
    }

    /// <summary>Thêm dimension vào OP (theo 06_dimensions_fai.md).</summary>
    [HttpPost("{opId:int}/dimensions")]
    [Authorize(Roles = "Administrator,Manager,Engineer,QC Inspector")]
    public async Task<IActionResult> CreateDimension(int opId, [FromBody] CreateDimensionRequest req)
    {
        var result = await mediator.Send(new CreateDimensionCommand(
            opId, req.BalloonNumber, req.Code, req.Description,
            req.NominalValue, req.TolerancePlus, req.ToleranceMinus,
            req.Unit, req.IsTextType, req.NominalText, req.CategoryId,
            req.IsCritical, req.IsFinal, req.SortOrder, UserId));
        return result.IsSuccess
            ? StatusCode(201, ApiResponse<DimensionDto>.Ok(result.Value))
            : BadRequest(ApiResponse<DimensionDto>.Fail(result.Errors));
    }

    /// <summary>Tính SPC (Cp, Cpk, mean, σ) cho một Dimension.</summary>
    [HttpGet("{opId:int}/dimensions/{dimId:long}/spc")]
    public async Task<IActionResult> GetSpc(long dimId)
    {
        var result = await mediator.Send(new GetSpcQuery(dimId));
        return result.IsSuccess
            ? Ok(ApiResponse<SpcDto>.Ok(result.Value))
            : BadRequest(ApiResponse<SpcDto>.Fail(result.Errors));
    }

    /// <summary>Import Operations từ file Excel — upsert theo OpNumber (cập nhật nếu đã tồn tại).</summary>
    [HttpPost("import")]
    [Authorize(Roles = "Administrator,Manager,Engineer")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportOps([FromForm] IFormFile file, [FromForm] int routingRevId)
    {
        await using var stream = file.OpenReadStream();
        var (_, rows) = ExcelImportReader.Read(stream);

        var importRows = rows.Select(row => new ImportOpRow(
            ExcelImportReader.Cell(row, "opnumber", "op") ?? string.Empty,
            ExcelImportReader.Cell(row, "optype"),
            ExcelImportReader.Cell(row, "description"),
            ExcelImportReader.CellDecimal(row, "setup", "setuptime"),
            ExcelImportReader.CellDecimal(row, "prod", "prodtime")
        )).ToList();

        var result = await mediator.Send(new ImportOpsCommand(routingRevId, importRows, UserId));
        return result.IsSuccess
            ? Ok(ApiResponse<ImportResultDto>.Ok(result.Value))
            : BadRequest(ApiResponse<ImportResultDto>.Fail(result.Errors));
    }

    /// <summary>Import Dimensions từ file Excel — bỏ qua balloon đã tồn tại.</summary>
    [HttpPost("{opId:int}/dimensions/import")]
    [Authorize(Roles = "Administrator,Manager,Engineer,QC Inspector")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportDimensions(int opId, [FromForm] IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        var (_, rows) = ExcelImportReader.Read(stream);

        var importRows = rows.Select(row => new ImportDimensionRow(
            ExcelImportReader.Cell(row, "balloonnumber", "balloon") ?? string.Empty,
            ExcelImportReader.Cell(row, "code"),
            ExcelImportReader.Cell(row, "description"),
            ExcelImportReader.Cell(row, "nominal"),
            ExcelImportReader.CellDecimal(row, "tolplus", "tol+"),
            ExcelImportReader.CellDecimal(row, "tolminus", "tol-"),
            ExcelImportReader.Cell(row, "unit"),
            ExcelImportReader.Cell(row, "category")
        )).ToList();

        var result = await mediator.Send(new ImportDimensionsCommand(opId, importRows, UserId));
        return result.IsSuccess
            ? Ok(ApiResponse<ImportResultDto>.Ok(result.Value))
            : BadRequest(ApiResponse<ImportResultDto>.Fail(result.Errors));
    }

    /// <summary>Tải file Excel mẫu cho Import Operations.</summary>
    [HttpGet("import/template")]
    public IActionResult GetOpsImportTemplate()
    {
        var bytes = ExcelTemplateBuilder.BuildOpsTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "import-ops-template.xlsx");
    }

    /// <summary>Tải file Excel mẫu cho Import Dimensions.</summary>
    [HttpGet("dimensions/import/template")]
    public IActionResult GetDimensionsImportTemplate()
    {
        var bytes = ExcelTemplateBuilder.BuildDimensionsTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "import-dimensions-template.xlsx");
    }
}

public record CreateOpRequest(
    int? RoutingRevId, int? JobId, string OpNumber, int? OpTypeId,
    string? Description, string? Note, decimal? SetupTime, decimal? ProdTime);

public record CreateDimensionRequest(
    string BalloonNumber, string? Code, string? Description,
    decimal? NominalValue, decimal? TolerancePlus, decimal? ToleranceMinus,
    string Unit,
    bool IsTextType = false, string? NominalText = null, int? CategoryId = null,
    bool IsCritical = false, bool IsFinal = false, int SortOrder = 0);

public record UpdateDimensionRequest(decimal? NominalValue, decimal? TolerancePlus, decimal? ToleranceMinus);
