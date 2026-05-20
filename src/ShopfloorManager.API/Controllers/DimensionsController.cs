using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopfloorManager.API.Common;
using ShopfloorManager.Application.Quality;

namespace ShopfloorManager.API.Controllers;

[ApiController]
[Route("api/v1/operations/{opId:int}/dimensions")]
[Authorize]
public class DimensionsController(IMediator mediator) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    /// <summary>Lấy danh sách dimensions của một operation.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<DimensionDto>>), 200)]
    public async Task<IActionResult> GetDimensions(int opId)
    {
        var result = await mediator.Send(new GetDimensionsQuery(opId));
        return Ok(ApiResponse<List<DimensionDto>>.Ok(result.Value));
    }

    /// <summary>Tạo dimension mới.</summary>
    [HttpPost]
    [Authorize(Roles = "Administrator,Manager,Engineer,QC Inspector")]
    [ProducesResponseType(typeof(ApiResponse<DimensionDto>), 201)]
    public async Task<IActionResult> CreateDimension(int opId, [FromBody] CreateDimensionRequest req)
    {
        var result = await mediator.Send(new CreateDimensionCommand(
            opId, req.Code, req.Description,
            req.Nominal, req.UpperTol, req.LowerTol,
            req.Unit, req.IsCritical, req.SortOrder, UserId));
        return result.IsSuccess
            ? StatusCode(201, ApiResponse<DimensionDto>.Ok(result.Value))
            : BadRequest(ApiResponse<DimensionDto>.Fail(result.Errors));
    }

    /// <summary>Cập nhật dimension.</summary>
    [HttpPut("{id:long}")]
    [Authorize(Roles = "Administrator,Manager,Engineer,QC Inspector")]
    [ProducesResponseType(typeof(ApiResponse<DimensionDto>), 200)]
    public async Task<IActionResult> UpdateDimension(int opId, long id, [FromBody] UpdateDimensionRequest req)
    {
        var result = await mediator.Send(new UpdateDimensionCommand(
            id, req.Description, req.Nominal, req.UpperTol, req.LowerTol, req.Unit, req.IsCritical, req.SortOrder));
        return result.IsSuccess
            ? Ok(ApiResponse<DimensionDto>.Ok(result.Value))
            : NotFound(ApiResponse<DimensionDto>.Fail(result.Errors));
    }

    /// <summary>Tính SPC (Cpk, Cp, mean, stddev) cho một dimension.</summary>
    [HttpGet("{id:long}/spc")]
    [ProducesResponseType(typeof(ApiResponse<SpcDto>), 200)]
    public async Task<IActionResult> GetSpc(long id)
    {
        var result = await mediator.Send(new GetSpcQuery(id));
        return result.IsSuccess
            ? Ok(ApiResponse<SpcDto>.Ok(result.Value))
            : BadRequest(ApiResponse<SpcDto>.Fail(result.Errors));
    }
}

public record CreateDimensionRequest(
    string Code, string? Description, decimal Nominal,
    decimal UpperTol, decimal LowerTol, string Unit,
    bool IsCritical, int SortOrder);

public record UpdateDimensionRequest(
    string? Description, decimal Nominal,
    decimal UpperTol, decimal LowerTol, string Unit,
    bool IsCritical, int SortOrder);
