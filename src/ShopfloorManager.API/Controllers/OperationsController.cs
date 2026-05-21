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

    /// <summary>Danh sách PartOps theo RoutingRevId (template).</summary>
    [HttpGet]
    public async Task<IActionResult> GetOps([FromQuery] int? routingRevId)
    {
        var result = await mediator.Send(new GetRoutingRevOpsQuery(routingRevId ?? 0));
        return Ok(ApiResponse<List<PartOpDto>>.Ok(result.Value));
    }

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

    /// <summary>Thêm dimension vào OP (theo 06_dimensions_fai.md).</summary>
    [HttpPost("{opId:int}/dimensions")]
    [Authorize(Roles = "Administrator,Manager,Engineer,QC Inspector")]
    public async Task<IActionResult> CreateDimension(int opId, [FromBody] CreateDimensionRequest req)
    {
        var result = await mediator.Send(new CreateDimensionCommand(
            opId, req.BalloonNumber, req.Code, req.Description,
            req.NominalValue, req.TolerancePlus, req.ToleranceMinus,
            req.Unit, req.IsTextType, req.NominalText, req.CategoryId,
            req.IsCritical, req.IsFinal, req.SortOrder, UserId));
        return result.IsSuccess
            ? StatusCode(201, ApiResponse<DimensionDto>.Ok(result.Value))
            : BadRequest(ApiResponse<DimensionDto>.Fail(result.Errors));
    }

    /// <summary>Tính SPC (Cp, Cpk, mean, σ) cho một Dimension.</summary>
    [HttpGet("{opId:int}/dimensions/{dimId:long}/spc")]
    public async Task<IActionResult> GetSpc(long dimId)
    {
        var result = await mediator.Send(new GetSpcQuery(dimId));
        return result.IsSuccess
            ? Ok(ApiResponse<SpcDto>.Ok(result.Value))
            : BadRequest(ApiResponse<SpcDto>.Fail(result.Errors));
    }
}

public record CreateOpRequest(
    int? RoutingRevId, int? JobId, string OpNumber, int? OpTypeId,
    string? Description, string? Note, decimal? SetupTime, decimal? ProdTime);

public record CreateDimensionRequest(
    string BalloonNumber, string? Code, string? Description,
    decimal? NominalValue, decimal? TolerancePlus, decimal? ToleranceMinus,
    string Unit,
    bool IsTextType = false, string? NominalText = null, int? CategoryId = null,
    bool IsCritical = false, bool IsFinal = false, int SortOrder = 0);
