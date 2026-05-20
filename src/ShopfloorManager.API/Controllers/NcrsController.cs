using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopfloorManager.API.Common;
using ShopfloorManager.Application.Quality;
using ShopfloorManager.Shared.Pagination;

namespace ShopfloorManager.API.Controllers;

[ApiController]
[Route("api/v1/ncrs")]
[Authorize]
public class NcrsController(IMediator mediator) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    /// <summary>Lấy danh sách NCR (phân trang, lọc theo status/job).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NcrDto>>), 200)]
    public async Task<IActionResult> GetNcrs([FromQuery] GetNcrsQuery query)
    {
        var result = await mediator.Send(query);
        var paged = result.Value;
        return Ok(new ApiResponse<IReadOnlyList<NcrDto>>
        {
            Success = true, Data = paged.Items,
            Pagination = new PaginationMeta(paged.Page, paged.PageSize, paged.Total, paged.TotalPages)
        });
    }

    /// <summary>Lấy chi tiết NCR kèm lịch sử actions.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<NcrDetailDto>), 200)]
    public async Task<IActionResult> GetNcr(long id)
    {
        var result = await mediator.Send(new GetNcrByIdQuery(id));
        return result.IsSuccess
            ? Ok(ApiResponse<NcrDetailDto>.Ok(result.Value))
            : NotFound(ApiResponse<NcrDetailDto>.Fail(result.Errors));
    }

    /// <summary>Tạo NCR mới.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<NcrDto>), 201)]
    public async Task<IActionResult> CreateNcr([FromBody] CreateNcrRequest req)
    {
        var result = await mediator.Send(
            new CreateNcrCommand(req.JobId, req.ProductId, req.PartOpId, req.Description, UserId));
        return result.IsSuccess
            ? StatusCode(201, ApiResponse<NcrDto>.Ok(result.Value))
            : BadRequest(ApiResponse<NcrDto>.Fail(result.Errors));
    }

    /// <summary>Thêm action vào NCR (Approve / Rework / Reject).</summary>
    [HttpPost("{id:long}/actions")]
    [Authorize(Roles = "Administrator,Manager,QC Inspector")]
    [ProducesResponseType(typeof(ApiResponse<NcrLogDto>), 201)]
    public async Task<IActionResult> AddAction(long id, [FromBody] NcrActionRequest req)
    {
        var result = await mediator.Send(new AddNcrActionCommand(id, req.Action, req.Note, UserId));
        return result.IsSuccess
            ? StatusCode(201, ApiResponse<NcrLogDto>.Ok(result.Value))
            : BadRequest(ApiResponse<NcrLogDto>.Fail(result.Errors));
    }
}

public record CreateNcrRequest(int JobId, int? ProductId, int? PartOpId, string Description);
public record NcrActionRequest(string Action, string? Note);
