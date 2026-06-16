using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopfloorManager.API.Common;
using ShopfloorManager.Application.Erp;
using ShopfloorManager.Application.Production;

namespace ShopfloorManager.API.Controllers;

[ApiController]
[Route("api/v1/erp")]
[Authorize(Roles = "Administrator,Manager,Engineer,Planner")]
public class ErpController(IMediator mediator) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    /// <summary>Danh sách kết nối ERP đang active.</summary>
    [HttpGet("connections")]
    public async Task<IActionResult> GetConnections()
    {
        var list = await mediator.Send(new GetErpConnectionsQuery());
        return Ok(ApiResponse<List<ErpConnectionDto>>.Ok(list));
    }

    /// <summary>Thêm kết nối ERP mới.</summary>
    [HttpPost("connections")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> CreateConnection([FromBody] CreateErpConnectionRequest req)
    {
        var result = await mediator.Send(
            new CreateErpConnectionCommand(req.Name, req.ErpType, req.BaseUrl, req.Company, req.Username, req.Password));
        return result.IsSuccess
            ? Ok(ApiResponse<ErpConnectionDto>.Ok(result.Value))
            : BadRequest(ApiResponse<ErpConnectionDto>.Fail(result.Errors));
    }

    /// <summary>Cập nhật kết nối ERP.</summary>
    [HttpPut("connections/{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> UpdateConnection(int id, [FromBody] UpdateErpConnectionRequest req)
    {
        var result = await mediator.Send(
            new UpdateErpConnectionCommand(id, req.Name, req.ErpType, req.BaseUrl,
                req.Company, req.Username, req.Password, req.IsActive));
        return result.IsSuccess
            ? Ok(ApiResponse<ErpConnectionDto>.Ok(result.Value))
            : BadRequest(ApiResponse<ErpConnectionDto>.Fail(result.Errors));
    }

    /// <summary>Kiểm tra kết nối ERP (ping).</summary>
    [HttpPost("connections/{id:int}/test")]
    public async Task<IActionResult> TestConnection(int id)
    {
        var result = await mediator.Send(new TestErpConnectionQuery(id));
        return result.IsSuccess
            ? Ok(ApiResponse<bool>.Ok(result.Value))
            : BadRequest(ApiResponse<bool>.Fail(result.Errors));
    }

    /// <summary>
    /// Lấy preview dữ liệu từ ERP (không import).
    /// Trả về danh sách row để user xem trước trên UI.
    /// </summary>
    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] ErpPreviewRequest req)
    {
        var result = await mediator.Send(
            new GetErpPreviewQuery(req.ConnectionId, req.DateFrom, req.DateTo, req.PoNumber));
        return result.IsSuccess
            ? Ok(ApiResponse<ErpPreviewDto>.Ok(result.Value))
            : BadRequest(ApiResponse<ErpPreviewDto>.Fail(result.Errors));
    }

    /// <summary>
    /// Import dữ liệu từ ERP — preview rồi chạy ImportJobBatchCommand.
    /// Reuse toàn bộ handler đã có sẵn.
    /// </summary>
    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] ErpPreviewRequest req)
    {
        // Bước 1: Lấy preview (fetch từ ERP)
        var previewResult = await mediator.Send(
            new GetErpPreviewQuery(req.ConnectionId, req.DateFrom, req.DateTo, req.PoNumber));
        if (!previewResult.IsSuccess)
            return BadRequest(ApiResponse<GlobalImportResultDto>.Fail(previewResult.Errors));

        // Bước 2: Map preview rows → ImportJobBatchRow
        var batchRows = previewResult.Value.Rows.Select(r => new ImportJobBatchRow(
            r.PartNumber, r.PartDescription, r.Revision,
            r.JobNumber, r.PoNumber, r.PoLine,
            r.RunQty,
            r.ShipBy != null ? DateOnly.TryParse(r.ShipBy, out var d) ? d : null : null,
            r.OpNumber, r.OpTypeCode, r.OpDescription,
            r.SetupTime, r.ProdTime)).ToList();

        // Bước 3: Import thông qua handler đã có
        var importResult = await mediator.Send(new ImportJobBatchCommand(batchRows, UserId));
        return importResult.IsSuccess
            ? Ok(ApiResponse<GlobalImportResultDto>.Ok(importResult.Value))
            : BadRequest(ApiResponse<GlobalImportResultDto>.Fail(importResult.Errors));
    }
}

public record CreateErpConnectionRequest(
    string Name, string ErpType, string BaseUrl,
    string? Company, string? Username, string? Password);

public record UpdateErpConnectionRequest(
    string Name, string ErpType, string BaseUrl,
    string? Company, string? Username, string? Password, bool IsActive);

public record ErpPreviewRequest(
    int ConnectionId,
    DateOnly? DateFrom, DateOnly? DateTo,
    string? PoNumber);
