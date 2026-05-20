using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopfloorManager.API.Common;
using ShopfloorManager.Application.Production;
using ShopfloorManager.Shared.Pagination;

namespace ShopfloorManager.API.Controllers;

[ApiController]
[Route("api/v1/jobs")]
[Authorize]
public class JobsController(IMediator mediator) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    [HttpGet]
    public async Task<IActionResult> GetJobs([FromQuery] GetJobsQuery query)
    {
        var result = await mediator.Send(query);
        var paged = result.Value;
        return Ok(new ApiResponse<IReadOnlyList<JobDto>>
        {
            Success = true, Data = paged.Items,
            Pagination = new PaginationMeta(paged.Page, paged.PageSize, paged.Total, paged.TotalPages)
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetJob(int id)
    {
        var result = await mediator.Send(new GetJobByIdQuery(id));
        return result.IsSuccess
            ? Ok(ApiResponse<JobDetailDto>.Ok(result.Value))
            : NotFound(ApiResponse<JobDetailDto>.Fail(result.Errors));
    }

    [HttpPost]
    [Authorize(Roles = "Administrator,Manager,Planner")]
    public async Task<IActionResult> CreateJob([FromBody] CreateJobRequest req)
    {
        var result = await mediator.Send(
            new CreateJobCommand(req.JobNumber, req.PartRevId, req.RoutingRevId, req.RunQty, req.ShipBy, UserId));
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetJob), new { id = result.Value.Id }, ApiResponse<JobDto>.Ok(result.Value))
            : BadRequest(ApiResponse<JobDto>.Fail(result.Errors));
    }

    /// <summary>
    /// Lấy danh sách Operations của Job:
    /// = RoutingRev template OPs + ForJobOnly OPs của job này.
    /// </summary>
    [HttpGet("{id:int}/operations")]
    public async Task<IActionResult> GetOperations(int id)
    {
        var result = await mediator.Send(new GetJobOpsQuery(id));
        return result.IsSuccess
            ? Ok(ApiResponse<List<PartOpDto>>.Ok(result.Value))
            : NotFound(ApiResponse<List<PartOpDto>>.Fail(result.Errors));
    }

    [HttpGet("{id:int}/products")]
    public async Task<IActionResult> GetProducts(int id)
    {
        var products = await mediator.Send(new GetJobsQuery());  // TODO: GetProductsQuery
        return Ok();
    }

    [HttpPost("{id:int}/products/generate")]
    [Authorize(Roles = "Administrator,Manager,Planner")]
    public async Task<IActionResult> GenerateProducts(int id, [FromBody] GenerateRequest req)
    {
        var result = await mediator.Send(new GenerateProductsCommand(id, req.Quantity));
        return result.IsSuccess
            ? StatusCode(201, ApiResponse<List<ProductDto>>.Ok(result.Value))
            : BadRequest(ApiResponse<List<ProductDto>>.Fail(result.Errors));
    }
}

public record CreateJobRequest(string JobNumber, int PartRevId, int RoutingRevId, int? RunQty, DateOnly? ShipBy);
public record GenerateRequest(int Quantity);
