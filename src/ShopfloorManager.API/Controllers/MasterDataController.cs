using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopfloorManager.API.Common;
using ShopfloorManager.Application.MasterData;

namespace ShopfloorManager.API.Controllers;

/// <summary>
/// CRUD cho dữ liệu danh mục nền tảng: Machine, MachineGroup, OpType, GageCategory, FileType.
/// </summary>
[ApiController]
[Authorize]
public class MasterDataController(IMediator mediator) : ControllerBase
{
    // ── Machines ──────────────────────────────────────────────

    [HttpGet("api/v1/machines")]
    [ProducesResponseType(typeof(ApiResponse<List<MachineDto>>), 200)]
    public async Task<IActionResult> GetMachines([FromQuery] bool activeOnly = true)
    {
        var result = await mediator.Send(new GetMachinesQuery(activeOnly));
        return Ok(ApiResponse<List<MachineDto>>.Ok(result.Value));
    }

    [HttpPost("api/v1/machines")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(ApiResponse<MachineDto>), 201)]
    public async Task<IActionResult> CreateMachine([FromBody] CreateMachineCommand command)
    {
        var result = await mediator.Send(command);
        return result.IsSuccess
            ? StatusCode(201, ApiResponse<MachineDto>.Ok(result.Value))
            : BadRequest(ApiResponse<MachineDto>.Fail(result.Errors));
    }

    [HttpPut("api/v1/machines/{id:int}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(ApiResponse<MachineDto>), 200)]
    public async Task<IActionResult> UpdateMachine(int id, [FromBody] UpdateMachineCommand command)
    {
        if (id != command.Id) return BadRequest(ApiResponse<MachineDto>.Fail("ID không khớp."));
        var result = await mediator.Send(command);
        return result.IsSuccess
            ? Ok(ApiResponse<MachineDto>.Ok(result.Value))
            : BadRequest(ApiResponse<MachineDto>.Fail(result.Errors));
    }

    // ── Machine Groups ────────────────────────────────────────

    [HttpGet("api/v1/machine-groups")]
    [ProducesResponseType(typeof(ApiResponse<List<MachineGroupDto>>), 200)]
    public async Task<IActionResult> GetMachineGroups([FromQuery] bool activeOnly = false)
    {
        var result = await mediator.Send(new GetMachineGroupsQuery(activeOnly));
        return Ok(ApiResponse<List<MachineGroupDto>>.Ok(result.Value));
    }

    [HttpPost("api/v1/machine-groups")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(ApiResponse<MachineGroupDto>), 201)]
    public async Task<IActionResult> CreateMachineGroup([FromBody] CreateMachineGroupCommand command)
    {
        var result = await mediator.Send(command);
        return result.IsSuccess
            ? StatusCode(201, ApiResponse<MachineGroupDto>.Ok(result.Value))
            : BadRequest(ApiResponse<MachineGroupDto>.Fail(result.Errors));
    }

    [HttpPut("api/v1/machine-groups/{id:int}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(ApiResponse<MachineGroupDto>), 200)]
    public async Task<IActionResult> UpdateMachineGroup(int id, [FromBody] UpdateMachineGroupCommand command)
    {
        if (id != command.Id) return BadRequest(ApiResponse<MachineGroupDto>.Fail("ID không khớp."));
        var result = await mediator.Send(command);
        return result.IsSuccess
            ? Ok(ApiResponse<MachineGroupDto>.Ok(result.Value))
            : BadRequest(ApiResponse<MachineGroupDto>.Fail(result.Errors));
    }

    // ── OpTypes ───────────────────────────────────────────────

    [HttpGet("api/v1/op-types")]
    [ProducesResponseType(typeof(ApiResponse<List<OpTypeDto>>), 200)]
    public async Task<IActionResult> GetOpTypes([FromQuery] bool activeOnly = false)
    {
        var result = await mediator.Send(new GetOpTypesQuery(activeOnly));
        return Ok(ApiResponse<List<OpTypeDto>>.Ok(result.Value));
    }

    [HttpPost("api/v1/op-types")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(ApiResponse<OpTypeDto>), 201)]
    public async Task<IActionResult> CreateOpType([FromBody] CreateOpTypeCommand command)
    {
        var result = await mediator.Send(command);
        return result.IsSuccess
            ? StatusCode(201, ApiResponse<OpTypeDto>.Ok(result.Value))
            : BadRequest(ApiResponse<OpTypeDto>.Fail(result.Errors));
    }

    [HttpPut("api/v1/op-types/{id:int}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(ApiResponse<OpTypeDto>), 200)]
    public async Task<IActionResult> UpdateOpType(int id, [FromBody] UpdateOpTypeCommand command)
    {
        if (id != command.Id) return BadRequest(ApiResponse<OpTypeDto>.Fail("ID không khớp."));
        var result = await mediator.Send(command);
        return result.IsSuccess
            ? Ok(ApiResponse<OpTypeDto>.Ok(result.Value))
            : BadRequest(ApiResponse<OpTypeDto>.Fail(result.Errors));
    }

    // ── GageCategories ────────────────────────────────────────

    [HttpGet("api/v1/gage-categories")]
    [ProducesResponseType(typeof(ApiResponse<List<GageCategoryDto>>), 200)]
    public async Task<IActionResult> GetGageCategories([FromQuery] bool activeOnly = false)
    {
        var result = await mediator.Send(new GetGageCategoriesQuery(activeOnly));
        return Ok(ApiResponse<List<GageCategoryDto>>.Ok(result.Value));
    }

    [HttpPost("api/v1/gage-categories")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(ApiResponse<GageCategoryDto>), 201)]
    public async Task<IActionResult> CreateGageCategory([FromBody] CreateGageCategoryCommand command)
    {
        var result = await mediator.Send(command);
        return result.IsSuccess
            ? StatusCode(201, ApiResponse<GageCategoryDto>.Ok(result.Value))
            : BadRequest(ApiResponse<GageCategoryDto>.Fail(result.Errors));
    }

    [HttpPut("api/v1/gage-categories/{id:int}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(ApiResponse<GageCategoryDto>), 200)]
    public async Task<IActionResult> UpdateGageCategory(int id, [FromBody] UpdateGageCategoryCommand command)
    {
        if (id != command.Id) return BadRequest(ApiResponse<GageCategoryDto>.Fail("ID không khớp."));
        var result = await mediator.Send(command);
        return result.IsSuccess
            ? Ok(ApiResponse<GageCategoryDto>.Ok(result.Value))
            : BadRequest(ApiResponse<GageCategoryDto>.Fail(result.Errors));
    }

    // ── FileTypes ─────────────────────────────────────────────

    [HttpGet("api/v1/tech-documents/file-types")]
    [ProducesResponseType(typeof(ApiResponse<List<FileTypeDto>>), 200)]
    public async Task<IActionResult> GetFileTypes([FromQuery] bool activeOnly = false)
    {
        var result = await mediator.Send(new GetFileTypesQuery(activeOnly));
        return Ok(ApiResponse<List<FileTypeDto>>.Ok(result.Value));
    }

    [HttpPost("api/v1/tech-documents/file-types")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(ApiResponse<FileTypeDto>), 201)]
    public async Task<IActionResult> CreateFileType([FromBody] CreateFileTypeCommand command)
    {
        var result = await mediator.Send(command);
        return result.IsSuccess
            ? StatusCode(201, ApiResponse<FileTypeDto>.Ok(result.Value))
            : BadRequest(ApiResponse<FileTypeDto>.Fail(result.Errors));
    }

    [HttpPut("api/v1/tech-documents/file-types/{id:int}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(ApiResponse<FileTypeDto>), 200)]
    public async Task<IActionResult> UpdateFileType(int id, [FromBody] UpdateFileTypeCommand command)
    {
        if (id != command.Id) return BadRequest(ApiResponse<FileTypeDto>.Fail("ID không khớp."));
        var result = await mediator.Send(command);
        return result.IsSuccess
            ? Ok(ApiResponse<FileTypeDto>.Ok(result.Value))
            : BadRequest(ApiResponse<FileTypeDto>.Fail(result.Errors));
    }
}
