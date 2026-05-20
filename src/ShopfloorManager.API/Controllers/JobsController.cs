using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopfloorManager.API.Common;
using ShopfloorManager.Application.Jobs;
using ShopfloorManager.Application.Operations;
using ShopfloorManager.Application.Products;
using ShopfloorManager.Shared.Pagination;

namespace ShopfloorManager.API.Controllers;

[ApiController]
[Route("api/v1/jobs")]
[Authorize]
public class JobsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<JobDto>>), 200)]
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
    [ProducesResponseType(typeof(ApiResponse<JobDetailDto>), 200)]
    public async Task<IActionResult> GetJob(int id)
    {
        var result = await mediator.Send(new GetJobByIdQuery(id));
        return result.IsSuccess ? Ok(ApiResponse<JobDetailDto>.Ok(result.Value))
            : NotFound(ApiResponse<JobDetailDto>.Fail(result.Errors));
    }

    [HttpPost]
    [Authorize(Roles = "Administrator,Manager,Planner")]
    [ProducesResponseType(typeof(ApiResponse<JobDto>), 201)]
    public async Task<IActionResult> CreateJob([FromBody] CreateJobCommand command)
    {
        var result = await mediator.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetJob), new { id = result.Value.Id }, ApiResponse<JobDto>.Ok(result.Value))
            : BadRequest(ApiResponse<JobDto>.Fail(result.Errors));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator,Manager,Planner")]
    [ProducesResponseType(typeof(ApiResponse<JobDto>), 200)]
    public async Task<IActionResult> UpdateJob(int id, [FromBody] UpdateJobCommand command)
    {
        var result = await mediator.Send(command with { Id = id });
        return result.IsSuccess ? Ok(ApiResponse<JobDto>.Ok(result.Value))
            : NotFound(ApiResponse<JobDto>.Fail(result.Errors));
    }

    // ── Nested: Operations ────────────────────────────────────

    [HttpGet("{id:int}/operations")]
    [ProducesResponseType(typeof(ApiResponse<List<PartOpDto>>), 200)]
    public async Task<IActionResult> GetOperations(int id)
    {
        var result = await mediator.Send(new GetPartOpsQuery(JobId: id));
        return Ok(ApiResponse<List<PartOpDto>>.Ok(result.Value));
    }

    // ── Nested: Products ──────────────────────────────────────

    [HttpGet("{id:int}/products")]
    [ProducesResponseType(typeof(ApiResponse<List<ProductDto>>), 200)]
    public async Task<IActionResult> GetProducts(int id)
    {
        var result = await mediator.Send(new GetProductsQuery(id));
        return Ok(ApiResponse<List<ProductDto>>.Ok(result.Value));
    }

    [HttpPost("{id:int}/products/generate")]
    [Authorize(Roles = "Administrator,Manager,Planner")]
    [ProducesResponseType(typeof(ApiResponse<List<ProductDto>>), 201)]
    public async Task<IActionResult> GenerateProducts(int id, [FromBody] GenerateRequest req)
    {
        var result = await mediator.Send(new GenerateProductsCommand(id, req.Quantity));
        return result.IsSuccess ? StatusCode(201, ApiResponse<List<ProductDto>>.Ok(result.Value))
            : BadRequest(ApiResponse<List<ProductDto>>.Fail(result.Errors));
    }
}

public record GenerateRequest(int Quantity);
