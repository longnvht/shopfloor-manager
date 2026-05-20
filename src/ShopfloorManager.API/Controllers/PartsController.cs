using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopfloorManager.API.Common;
using ShopfloorManager.Application.Production;
using ShopfloorManager.Shared.Pagination;

namespace ShopfloorManager.API.Controllers;

[ApiController]
[Route("api/v1/parts")]
[Authorize]
public class PartsController(IMediator mediator) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    /// <summary>Danh sách Parts (phân trang, tìm kiếm).</summary>
    [HttpGet]
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

    /// <summary>Tạo Part mới (tự động tạo Rev A + Routing Standard R1).</summary>
    [HttpPost]
    [Authorize(Roles = "Administrator,Manager,Engineer")]
    public async Task<IActionResult> CreatePart([FromBody] CreatePartRequest req)
    {
        var result = await mediator.Send(new CreatePartCommand(req.PartNumber, req.Description, req.RevCode, UserId));
        return result.IsSuccess
            ? StatusCode(201, ApiResponse<PartRevDto>.Ok(result.Value))
            : BadRequest(ApiResponse<PartRevDto>.Fail(result.Errors));
    }

    /// <summary>Lấy tất cả revisions của một Part.</summary>
    [HttpGet("{partId:int}/revisions")]
    public async Task<IActionResult> GetRevisions(int partId)
    {
        var result = await mediator.Send(new GetPartRevsQuery(partId));
        return Ok(ApiResponse<List<PartRevDto>>.Ok(result.Value));
    }

    /// <summary>Thêm revision mới cho Part (tự động tạo Routing Standard R1).</summary>
    [HttpPost("{partId:int}/revisions")]
    [Authorize(Roles = "Administrator,Manager,Engineer")]
    public async Task<IActionResult> AddRevision(int partId, [FromBody] AddRevisionRequest req)
    {
        var result = await mediator.Send(new AddPartRevCommand(partId, req.RevCode, req.Description, UserId));
        return result.IsSuccess
            ? StatusCode(201, ApiResponse<PartRevDto>.Ok(result.Value))
            : BadRequest(ApiResponse<PartRevDto>.Fail(result.Errors));
    }

    /// <summary>Lấy danh sách RoutingRevs của một PartRev.</summary>
    [HttpGet("revisions/{partRevId:int}/routing-revs")]
    public async Task<IActionResult> GetRoutingRevs(int partRevId, [FromQuery] int routingId)
    {
        var result = await mediator.Send(new GetRoutingRevsQuery(routingId));
        return Ok(ApiResponse<List<RoutingRevDto>>.Ok(result.Value));
    }

    /// <summary>Tạo RoutingRev mới (copy OPs từ rev đang active).</summary>
    [HttpPost("routing-revs")]
    [Authorize(Roles = "Administrator,Manager,Engineer")]
    public async Task<IActionResult> AddRoutingRev([FromBody] AddRoutingRevRequest req)
    {
        var result = await mediator.Send(new AddRoutingRevCommand(req.RoutingId, req.RevCode, req.ChangeNote, UserId));
        return result.IsSuccess
            ? StatusCode(201, ApiResponse<RoutingRevDto>.Ok(result.Value))
            : BadRequest(ApiResponse<RoutingRevDto>.Fail(result.Errors));
    }
}

public record CreatePartRequest(string PartNumber, string Description, string RevCode = "A");
public record AddRevisionRequest(string RevCode, string? Description);
public record AddRoutingRevRequest(int RoutingId, string RevCode, string? ChangeNote);
