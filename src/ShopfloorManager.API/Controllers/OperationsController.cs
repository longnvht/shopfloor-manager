using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopfloorManager.API.Common;
using ShopfloorManager.Application.Operations;

namespace ShopfloorManager.API.Controllers;

[ApiController]
[Route("api/v1/operations")]
[Authorize]
public class OperationsController(IMediator mediator) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "0");

    /// <summary>Lấy danh sách operations theo part hoặc job.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PartOpDto>>), 200)]
    public async Task<IActionResult> GetOperations([FromQuery] GetPartOpsQuery query)
    {
        var result = await mediator.Send(query);
        return Ok(ApiResponse<List<PartOpDto>>.Ok(result.Value));
    }

    /// <summary>Tạo operation mới.</summary>
    [HttpPost]
    [Authorize(Roles = "Administrator,Manager,Engineer")]
    [ProducesResponseType(typeof(ApiResponse<PartOpDto>), 201)]
    public async Task<IActionResult> CreateOperation([FromBody] CreatePartOpRequest req)
    {
        var result = await mediator.Send(new CreatePartOpCommand(
            req.OpNumber, req.PartId, req.JobId, req.OpTypeId,
            req.Description, req.Note, req.SetupTime, req.ProdTime, UserId));
        return result.IsSuccess
            ? StatusCode(201, ApiResponse<PartOpDto>.Ok(result.Value))
            : BadRequest(ApiResponse<PartOpDto>.Fail(result.Errors));
    }
}

public record CreatePartOpRequest(
    string OpNumber, int? PartId, int? JobId, int? OpTypeId,
    string? Description, string? Note, decimal? SetupTime, decimal? ProdTime);
