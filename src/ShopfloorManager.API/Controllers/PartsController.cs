using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopfloorManager.API.Common;
using ShopfloorManager.Application.Parts;
using ShopfloorManager.Shared.Pagination;

namespace ShopfloorManager.API.Controllers;

[ApiController]
[Route("api/v1/parts")]
[Authorize]
public class PartsController(IMediator mediator) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "0");

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PartDto>>), 200)]
    public async Task<IActionResult> GetParts([FromQuery] GetPartsQuery query)
    {
        var result = await mediator.Send(query);
        var paged = result.Value;
        return Ok(new ApiResponse<IReadOnlyList<PartDto>>
        {
            Success = true, Data = paged.Items,
            Pagination = new PaginationMeta(paged.Page, paged.PageSize, paged.Total, paged.TotalPages)
        });
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<PartDto>), 200)]
    public async Task<IActionResult> GetPart(int id)
    {
        var result = await mediator.Send(new GetPartByIdQuery(id));
        return result.IsSuccess ? Ok(ApiResponse<PartDto>.Ok(result.Value))
            : NotFound(ApiResponse<PartDto>.Fail(result.Errors));
    }

    [HttpPost]
    [Authorize(Roles = "Administrator,Manager,Engineer")]
    [ProducesResponseType(typeof(ApiResponse<PartDto>), 201)]
    public async Task<IActionResult> CreatePart([FromBody] CreatePartRequest req)
    {
        var result = await mediator.Send(new CreatePartCommand(req.PartNumber, req.Description, req.Revision, UserId));
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetPart), new { id = result.Value.Id }, ApiResponse<PartDto>.Ok(result.Value))
            : BadRequest(ApiResponse<PartDto>.Fail(result.Errors));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator,Manager,Engineer")]
    [ProducesResponseType(typeof(ApiResponse<PartDto>), 200)]
    public async Task<IActionResult> UpdatePart(int id, [FromBody] UpdatePartRequest req)
    {
        var result = await mediator.Send(
            new UpdatePartCommand(id, req.Description, req.Revision, req.RoutingRevision, req.IsActive, UserId));
        return result.IsSuccess ? Ok(ApiResponse<PartDto>.Ok(result.Value))
            : NotFound(ApiResponse<PartDto>.Fail(result.Errors));
    }
}

public record CreatePartRequest(string PartNumber, string Description, string? Revision);
public record UpdatePartRequest(string Description, string? Revision, string? RoutingRevision, bool IsActive);
