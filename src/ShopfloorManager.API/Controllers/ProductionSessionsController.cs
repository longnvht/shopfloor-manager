using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopfloorManager.API.Common;
using ShopfloorManager.Application.Production;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Shared.Constants;

namespace ShopfloorManager.API.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class ProductionSessionsController(IMediator mediator) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

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

    /// <summary>Operator chọn sản phẩm — tạo session (Claimed).</summary>
    [HttpPost("production-sessions")]
    public async Task<IActionResult> Claim([FromBody] ClaimSessionRequest request)
    {
        var result = await mediator.Send(new ClaimSessionCommand(
            request.ProductId, request.PartOpId, request.MachineCode, UserId));
        return result.IsSuccess
            ? Ok(ApiResponse<ProductionSessionDto>.Ok(result.Value))
            : BadRequest(ApiResponse<ProductionSessionDto>.Fail(result.Errors));
    }

    /// <summary>Bấm "Bắt đầu" — ghi nhận thời gian bắt đầu gia công.</summary>
    [HttpPut("production-sessions/{id:int}/start")]
    public async Task<IActionResult> Start(int id)
    {
        var result = await mediator.Send(new StartSessionCommand(id));
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

public record ClaimSessionRequest(int ProductId, int PartOpId, string MachineCode);
