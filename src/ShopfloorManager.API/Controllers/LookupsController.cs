using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.API.Common;
using ShopfloorManager.Application.Lookups;
using ShopfloorManager.Application.MasterData;

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

    // ── OpTypes ───────────────────────────────────────────────

    [HttpGet("api/v1/op-types")]
    public async Task<IActionResult> GetOpTypes(
        [Microsoft.AspNetCore.Mvc.FromServices] ShopfloorManager.Application.Common.Interfaces.IShopfloorDbContext db)
    {
        var items = await db.OpTypes
            .OrderBy(t => t.Code)
            .Select(t => new { t.Id, t.Code, t.Name })
            .ToListAsync();
        return Ok(ApiResponse<object>.Ok(items));
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

    // ── DimensionCategories ───────────────────────────────────

    [HttpGet("api/v1/dimension-categories")]
    public async Task<IActionResult> GetDimensionCategories(
        [Microsoft.AspNetCore.Mvc.FromServices] ShopfloorManager.Application.Common.Interfaces.IShopfloorDbContext db)
    {
        var items = await db.DimensionCategories
            .OrderBy(c => c.Code)
            .Select(c => new { c.Id, c.Code, c.Name, c.Description })
            .ToListAsync();
        return Ok(ApiResponse<object>.Ok(items));
    }

    // ── Machines ──────────────────────────────────────────────

    [HttpGet("api/v1/machines")]
    [ProducesResponseType(typeof(ApiResponse<List<MachineDto>>), 200)]
    public async Task<IActionResult> GetMachines([FromQuery] bool activeOnly = true)
    {
        var result = await mediator.Send(new GetMachinesQuery(activeOnly));
        return Ok(ApiResponse<List<MachineDto>>.Ok(result.Value));
    }

    [HttpGet("api/v1/machine-groups")]
    public async Task<IActionResult> GetMachineGroups()
    {
        var result = await mediator.Send(new GetMachineGroupsQuery());
        return Ok(ApiResponse<List<MachineGroupDto>>.Ok(result.Value));
    }

    // ── FileTypes ─────────────────────────────────────────────

    [HttpGet("api/v1/tech-documents/file-types")]
    public async Task<IActionResult> GetFileTypes(
        [Microsoft.AspNetCore.Mvc.FromServices] ShopfloorManager.Application.Common.Interfaces.IShopfloorDbContext db)
    {
        var items = await db.FileTypes
            .OrderBy(f => f.SortOrder)
            .Select(f => new {
                f.Id, f.Code, f.Name, f.Folder,
                f.IsPartNumber, f.IsRevision, f.IsOpNumber, f.IsJobNumber,
                f.IsGcode, f.IsSegment, f.SortOrder })
            .ToListAsync();
        return Ok(ApiResponse<object>.Ok(items));
    }
}
