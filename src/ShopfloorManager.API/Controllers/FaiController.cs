using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopfloorManager.API.Common;
using ShopfloorManager.Application.Quality;

namespace ShopfloorManager.API.Controllers;

[ApiController]
[Route("api/v1/fai")]
[Authorize]
public class FaiController(IMediator mediator) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    /// <summary>Lấy FAI sheet (bảng đo) cho một operation + job.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<FaiSheetDto>), 200)]
    public async Task<IActionResult> GetFaiSheet([FromQuery] int partOpId, [FromQuery] int jobId)
    {
        var result = await mediator.Send(new GetFaiSheetQuery(partOpId, jobId));
        return result.IsSuccess
            ? Ok(ApiResponse<FaiSheetDto>.Ok(result.Value))
            : BadRequest(ApiResponse<FaiSheetDto>.Fail(result.Errors));
    }

    /// <summary>Lưu một giá trị đo (tạo mới hoặc cập nhật).</summary>
    [HttpPost("measure")]
    [Authorize(Roles = "Administrator,Manager,Engineer,QC Inspector,Operator")]
    [ProducesResponseType(typeof(ApiResponse<MeasureValueDto>), 200)]
    public async Task<IActionResult> SaveMeasure([FromBody] SaveMeasureRequest req)
    {
        var result = await mediator.Send(new SaveMeasureCommand(
            req.DimensionId, req.ProductId, req.Value, req.Note, UserId));
        return result.IsSuccess
            ? Ok(ApiResponse<MeasureValueDto>.Ok(result.Value))
            : BadRequest(ApiResponse<MeasureValueDto>.Fail(result.Errors));
    }
}

public record SaveMeasureRequest(long DimensionId, int ProductId, decimal Value, string? Note);
