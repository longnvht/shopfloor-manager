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

    /// <summary>Thêm dimension vào OP.</summary>
    [HttpPost("{opId:int}/dimensions")]
    [Authorize(Roles = "Administrator,Manager,Engineer,QC Inspector")]
    public async Task<IActionResult> CreateDimension(int opId, [FromBody] CreateDimensionRequest req)
    {
        var result = await mediator.Send(new CreateDimensionCommand(
            opId, req.BalloonNumber, req.Code, req.Description,
            req.Nominal, req.UpperTol, req.LowerTol,
            req.Unit, req.IsCritical, req.SortOrder, UserId));
        return result.IsSuccess
            ? StatusCode(201, ApiResponse<DimensionDto>.Ok(result.Value))
            : BadRequest(ApiResponse<DimensionDto>.Fail(result.Errors));
    }
}

public record CreateOpRequest(
    int? RoutingRevId, int? JobId, string OpNumber, int? OpTypeId,
    string? Description, string? Note, decimal? SetupTime, decimal? ProdTime);

public record CreateDimensionRequest(
    string BalloonNumber, string? Code, string? Description,
    decimal Nominal, decimal UpperTol, decimal LowerTol,
    string Unit, bool IsCritical, int SortOrder);
