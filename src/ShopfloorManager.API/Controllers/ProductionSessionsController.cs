using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopfloorManager.API.Common;
using ShopfloorManager.Application.Production;
using ShopfloorManager.Shared;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Shared.Constants;

namespace ShopfloorManager.API.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class ProductionSessionsController(IMediator mediator) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    /// <summary>Tổng hợp theo ngày cho 1 máy: số SP hoàn thành, thời gian hoạt động, pass/fail rate.</summary>
    [HttpGet("machines/{machineCode}/daily-summary")]
    [ProducesResponseType(typeof(ApiResponse<DailySummaryDto>), 200)]
    public async Task<IActionResult> GetDailySummary(string machineCode, [FromQuery] DateOnly? date = null)
    {
        var queryDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await mediator.Send(new GetDailySummaryQuery(machineCode, queryDate));
        return Ok(ApiResponse<DailySummaryDto>.Ok(result.Value));
    }

    /// <summary>Lấy session đang mở trên máy (để kiểm tra khi login).</summary>
    [HttpGet("machines/{machineCode}/active-session")]
    public async Task<IActionResult> GetActiveSession(string machineCode)
    {
        var result = await mediator.Send(new GetActiveSessionQuery(machineCode));
        return result.IsSuccess
            ? Ok(ApiResponse<ActiveSessionDto?>.Ok(result.Value))
            : BadRequest(ApiResponse<ActiveSessionDto?>.Fail(result.Errors));
    }

    /// <summary>Danh sách sản phẩm kèm trạng thái session của từng OP.</summary>
    [HttpGet("jobs/{jobId:int}/operations/{opId:int}/products")]
    public async Task<IActionResult> GetProductsWithSession(int jobId, int opId)
    {
        var result = await mediator.Send(new GetProductsWithSessionQuery(jobId, opId));
        return result.IsSuccess
            ? Ok(ApiResponse<List<ProductWithSessionDto>>.Ok(result.Value))
            : BadRequest(ApiResponse<List<ProductWithSessionDto>>.Fail(result.Errors));
    }

    /// <summary>Operator bấm "Bắt đầu" — tạo session và bắt đầu gia công ngay.</summary>
    [HttpPost("production-sessions")]
    [Authorize(Roles = $"{AppConstants.Roles.Operator},{AppConstants.Roles.Leader},{AppConstants.Roles.Admin}")]
    public async Task<IActionResult> Begin([FromBody] BeginSessionRequest request)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "";
        var result = await mediator.Send(new BeginSessionCommand(
            request.ProductId, request.PartOpId, request.MachineCode, UserId, role));
        return result.IsSuccess
            ? Ok(ApiResponse<ProductionSessionDto>.Ok(result.Value))
            : BadRequest(ApiResponse<ProductionSessionDto>.Fail(result.Errors));
    }

    /// <summary>Bấm "Kết thúc" — hoàn thành phiên gia công.</summary>
    [HttpPut("production-sessions/{id:int}/complete")]
    public async Task<IActionResult> Complete(int id)
    {
        var result = await mediator.Send(new CompleteSessionCommand(id));
        return result.IsSuccess
            ? Ok(ApiResponse<ProductionSessionDto>.Ok(result.Value))
            : BadRequest(ApiResponse<ProductionSessionDto>.Fail(result.Errors));
    }

    /// <summary>Supervisor unlock — huỷ phiên đang mở.</summary>
    [HttpPut("production-sessions/{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelSessionCommand command)
    {
        if (id != command.SessionId)
            return BadRequest(ApiResponse<ProductionSessionDto>.Fail("Id không khớp."));
        var result = await mediator.Send(command);
        return result.IsSuccess
            ? Ok(ApiResponse<ProductionSessionDto>.Ok(result.Value))
            : BadRequest(ApiResponse<ProductionSessionDto>.Fail(result.Errors));
    }

    /// <summary>Leader/Admin force-complete phiên của người khác.</summary>
    [HttpPut("production-sessions/{id:int}/force-complete")]
    [Authorize(Roles = $"{AppConstants.Roles.Leader},{AppConstants.Roles.Admin},{AppConstants.Roles.Manager}")]
    public async Task<IActionResult> ForceComplete(int id)
    {
        var result = await mediator.Send(new ForceCompleteSessionCommand(id, UserId));
        return result.IsSuccess
            ? Ok(ApiResponse<ProductionSessionDto>.Ok(result.Value))
            : BadRequest(ApiResponse<ProductionSessionDto>.Fail(result.Errors));
    }
}

public record BeginSessionRequest(int ProductId, int PartOpId, string MachineCode);
