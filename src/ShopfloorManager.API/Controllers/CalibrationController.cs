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
public class CalibrationController(IMediator mediator) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    // ── Vendors ───────────────────────────────────────────────────────────

    [HttpGet("calib-vendors")]
    public async Task<IActionResult> GetVendors()
    {
        var result = await mediator.Send(new GetCalibVendorsQuery());
        return Ok(ApiResponse<List<CalibVendorDto>>.Ok(result.Value));
    }

    [HttpPost("calib-vendors")]
    public async Task<IActionResult> CreateVendor([FromBody] CreateCalibVendorCommand command)
    {
        var result = await mediator.Send(command);
        return result.IsSuccess
            ? StatusCode(201, ApiResponse<CalibVendorDto>.Ok(result.Value))
            : BadRequest(ApiResponse<CalibVendorDto>.Fail(result.Errors));
    }

    // ── Calib Requests ────────────────────────────────────────────────────

    [HttpGet("calib-requests")]
    public async Task<IActionResult> GetRequests(
        [FromQuery] CalibRequestStatus? status, [FromQuery] int? gageId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 30)
    {
        var result = await mediator.Send(new GetCalibRequestsQuery(status, gageId, page, pageSize));
        return Ok(ApiResponse<List<CalibRequestDto>>.Ok(result.Value));
    }

    [HttpPost("calib-requests")]
    public async Task<IActionResult> CreateRequest([FromBody] CreateCalibRequestBody body)
    {
        var result = await mediator.Send(new CreateCalibRequestCommand(body.GageId, body.VendorId, UserId));
        return result.IsSuccess
            ? StatusCode(201, ApiResponse<int>.Ok(result.Value))
            : BadRequest(ApiResponse<int>.Fail(result.Errors));
    }

    [HttpPut("calib-requests/{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var result = await mediator.Send(new ApproveCalibRequestCommand(id));
        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(null))
            : BadRequest(ApiResponse<object>.Fail(result.Errors));
    }

    // ── Calib Records (Complete calibration) ──────────────────────────────

    [HttpPost("calib-records")]
    public async Task<IActionResult> Complete([FromBody] CompleteCalibrationCommand command)
    {
        // Inject userId
        var cmd = command with { CreatedBy = UserId };
        var result = await mediator.Send(cmd);
        return result.IsSuccess
            ? StatusCode(201, ApiResponse<int>.Ok(result.Value))
            : BadRequest(ApiResponse<int>.Fail(result.Errors));
    }
}

public record CreateCalibRequestBody(int GageId, int? VendorId);
