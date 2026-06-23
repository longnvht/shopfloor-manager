using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopfloorManager.API.Common;
using ShopfloorManager.Application.GageManagement;
using ShopfloorManager.Domain.Enums;

namespace ShopfloorManager.API.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class GagesController(IMediator mediator) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    // ── Gage master ──────────────────────────────────────────────────────

    [HttpGet("gages")]
    public async Task<IActionResult> GetGages(
        [FromQuery] string? search, [FromQuery] string? statusCode,
        [FromQuery] int? gageTypeId, [FromQuery] bool? isBorrowed,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var result = await mediator.Send(new GetGagesQuery(search, statusCode, gageTypeId, isBorrowed, page, pageSize));
        return Ok(ApiResponse<List<GageDto>>.Ok(result.Value));
    }

    /// <summary>MES (Desktop): danh sách gage hợp lệ, chưa bị mượn — chọn khi nhập measure value.</summary>
    [HttpGet("mes/gages")]
    public async Task<IActionResult> GetMesGages([FromQuery] string? categoryCode)
    {
        var result = await mediator.Send(new GetMesGagesQuery(categoryCode));
        return Ok(ApiResponse<List<MesGageDto>>.Ok(result.Value));
    }

    [HttpGet("gages/calib-due")]
    public async Task<IActionResult> GetCalibDue([FromQuery] int days = 60)
    {
        var result = await mediator.Send(new GetGagesCalibDueQuery(days));
        return Ok(ApiResponse<List<GageDto>>.Ok(result.Value));
    }

    [HttpPost("gages")]
    public async Task<IActionResult> CreateGage([FromBody] CreateGageCommand command)
    {
        var result = await mediator.Send(command);
        return result.IsSuccess
            ? StatusCode(201, ApiResponse<GageDto>.Ok(result.Value))
            : BadRequest(ApiResponse<GageDto>.Fail(result.Errors));
    }

    // ── Gage Types / Locations ────────────────────────────────────────────

    [HttpGet("gage-types")]
    public async Task<IActionResult> GetGageTypes([FromQuery] int? categoryId)
    {
        var result = await mediator.Send(new GetGageTypesQuery(categoryId));
        return Ok(ApiResponse<List<GageTypeDto>>.Ok(result.Value));
    }

    [HttpGet("gage-locations")]
    public async Task<IActionResult> GetGageLocations()
    {
        var result = await mediator.Send(new GetGageLocationsQuery());
        return Ok(ApiResponse<List<GageLocationDto>>.Ok(result.Value));
    }

    // ── Borrow / Return ───────────────────────────────────────────────────

    [HttpGet("borrow-transactions")]
    public async Task<IActionResult> GetBorrowTransactions(
        [FromQuery] int? gageId, [FromQuery] BorrowStatus? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var result = await mediator.Send(new GetBorrowTransactionsQuery(gageId, status, page, pageSize));
        return Ok(ApiResponse<List<BorrowTransactionDto>>.Ok(result.Value));
    }

    [HttpPost("borrow-transactions")]
    public async Task<IActionResult> Borrow([FromBody] BorrowGageCommand command)
    {
        var result = await mediator.Send(command);
        return result.IsSuccess
            ? Ok(ApiResponse<int>.Ok(result.Value))
            : BadRequest(ApiResponse<int>.Fail(result.Errors));
    }

    [HttpPut("borrow-transactions/{id:int}/return")]
    public async Task<IActionResult> Return(int id)
    {
        var result = await mediator.Send(new ReturnGageCommand(id));
        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(null))
            : BadRequest(ApiResponse<object>.Fail(result.Errors));
    }
}
