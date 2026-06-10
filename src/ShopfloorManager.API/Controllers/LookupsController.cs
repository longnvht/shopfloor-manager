using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.API.Common;
using ShopfloorManager.Application.Lookups;

namespace ShopfloorManager.API.Controllers;

/// <summary>
/// Các bảng danh mục nhỏ: positions, user-types, work-statuses.
/// </summary>
[ApiController]
[Authorize]
public class LookupsController(IMediator mediator) : ControllerBase
{
    // ── Positions ─────────────────────────────────────────────

    [HttpGet("api/v1/positions")]
    [ProducesResponseType(typeof(ApiResponse<List<PositionDto>>), 200)]
    public async Task<IActionResult> GetPositions()
    {
        var result = await mediator.Send(new GetPositionsQuery());
        return Ok(ApiResponse<List<PositionDto>>.Ok(result.Value));
    }

    [HttpPost("api/v1/positions")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(ApiResponse<PositionDto>), 201)]
    public async Task<IActionResult> CreatePosition([FromBody] CreatePositionCommand command)
    {
        var result = await mediator.Send(command);
        return result.IsSuccess
            ? StatusCode(201, ApiResponse<PositionDto>.Ok(result.Value))
            : BadRequest(ApiResponse<PositionDto>.Fail(result.Errors));
    }

    // ── UserTypes ─────────────────────────────────────────────

    [HttpGet("api/v1/user-types")]
    [ProducesResponseType(typeof(ApiResponse<List<UserTypeDto>>), 200)]
    public async Task<IActionResult> GetUserTypes()
    {
        var result = await mediator.Send(new GetUserTypesQuery());
        return Ok(ApiResponse<List<UserTypeDto>>.Ok(result.Value));
    }

    [HttpPost("api/v1/user-types")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(ApiResponse<UserTypeDto>), 201)]
    public async Task<IActionResult> CreateUserType([FromBody] CreateUserTypeCommand command)
    {
        var result = await mediator.Send(command);
        return result.IsSuccess
            ? StatusCode(201, ApiResponse<UserTypeDto>.Ok(result.Value))
            : BadRequest(ApiResponse<UserTypeDto>.Fail(result.Errors));
    }

    // ── WorkStatuses ──────────────────────────────────────────

    [HttpGet("api/v1/work-statuses")]
    [ProducesResponseType(typeof(ApiResponse<List<WorkStatusDto>>), 200)]
    public async Task<IActionResult> GetWorkStatuses()
    {
        var result = await mediator.Send(new GetWorkStatusesQuery());
        return Ok(ApiResponse<List<WorkStatusDto>>.Ok(result.Value));
    }

    // ── NcrReasons ────────────────────────────────────────────

    [HttpGet("api/v1/ncr-reasons")]
    public async Task<IActionResult> GetNcrReasons(
        [Microsoft.AspNetCore.Mvc.FromServices] ShopfloorManager.Application.Common.Interfaces.IShopfloorDbContext db,
        [FromQuery] int? departmentId = null)
    {
        var q = db.NcrReasons.Where(r => r.IsActive).AsQueryable();
        if (departmentId.HasValue) q = q.Where(r => r.DepartmentId == departmentId.Value);
        var items = await q.OrderBy(r => r.SortOrder)
            .Select(r => new { r.Id, r.Name, r.Tag, r.DepartmentId, r.SortOrder })
            .ToListAsync();
        return Ok(ApiResponse<object>.Ok(items));
    }
}
