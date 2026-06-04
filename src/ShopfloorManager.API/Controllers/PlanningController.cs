using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopfloorManager.API.Common;
using ShopfloorManager.Application.Planning;

namespace ShopfloorManager.API.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class PlanningController(IMediator mediator) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    [HttpGet("planning")]
    public async Task<IActionResult> GetItems(
        [FromQuery] DateTimeOffset? startDate,
        [FromQuery] DateTimeOffset? endDate,
        [FromQuery] int? machineId)
    {
        var result = await mediator.Send(new GetPlanningItemsQuery(startDate, endDate, machineId));
        return Ok(ApiResponse<List<PlanningItemDto>>.Ok(result.Value));
    }

    [HttpPost("planning")]
    public async Task<IActionResult> CreateItem([FromBody] CreatePlanningItemBody body)
    {
        var result = await mediator.Send(new CreatePlanningItemCommand(
            body.JobId, body.PartOpId, body.MachineId,
            body.OperatorId, body.ShiftId,
            body.StartTime, body.EndTime, body.Note, UserId));
        if (!result.IsSuccess) return BadRequest(ApiResponse<PlanningItemDto>.Fail(result.Errors));
        return StatusCode(201, ApiResponse<PlanningItemDto>.Ok(result.Value));
    }

    [HttpDelete("planning/{id:int}")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        var result = await mediator.Send(new DeletePlanningItemCommand(id));
        return result.IsSuccess ? Ok(ApiResponse<object>.Ok(null)) : BadRequest(ApiResponse<object>.Fail(result.Errors));
    }

    [HttpGet("shifts")]
    public async Task<IActionResult> GetShifts()
    {
        var result = await mediator.Send(new GetShiftsQuery());
        return Ok(ApiResponse<List<ShiftDto>>.Ok(result.Value));
    }

    [HttpPost("shifts")]
    public async Task<IActionResult> CreateShift([FromBody] CreateShiftCommand command)
    {
        var result = await mediator.Send(command);
        return result.IsSuccess
            ? StatusCode(201, ApiResponse<ShiftDto>.Ok(result.Value))
            : BadRequest(ApiResponse<ShiftDto>.Fail(result.Errors));
    }

    [HttpGet("break-times")]
    public async Task<IActionResult> GetBreakTimes()
    {
        var result = await mediator.Send(new GetBreakTimesQuery());
        return Ok(ApiResponse<List<BreakTimeDto>>.Ok(result.Value));
    }
}

public record CreatePlanningItemBody(
    int JobId, int PartOpId, int MachineId,
    int? OperatorId, int? ShiftId,
    DateTimeOffset StartTime, DateTimeOffset EndTime,
    string? Note);
