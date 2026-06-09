using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopfloorManager.API.Common;
using ShopfloorManager.Application.Production;

namespace ShopfloorManager.API.Controllers;

[ApiController]
[Route("api/v1/fai")]
[Authorize]
public class FaiController(IMediator mediator) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    /// <summary>FAI Sheet: ma trận (Serials × Dimensions) cho một OP của một Job.</summary>
    [HttpGet]
    public async Task<IActionResult> GetFaiSheet([FromQuery] int jobId, [FromQuery] int partOpId)
    {
        var result = await mediator.Send(new GetFaiSheetQuery(jobId, partOpId));
        return result.IsSuccess
            ? Ok(ApiResponse<FaiSheetDto>.Ok(result.Value))
            : BadRequest(ApiResponse<FaiSheetDto>.Fail(result.Errors));
    }

    /// <summary>
    /// Nhập giá trị đo — tạo record mới mỗi lần (giữ lịch sử).
    /// Số: auto Pass/Fail vs MaxValue/MinValue.
    /// Text: operator truyền ManualResult=true(Pass)/false(Fail).
    /// </summary>
    [HttpPost("measure")]
    [Authorize(Roles = "Administrator,Manager,Engineer,QC Inspector,Operator")]
    public async Task<IActionResult> SaveMeasure([FromBody] SaveMeasureRequest req)
    {
        var result = await mediator.Send(new SaveMeasureCommand(
            req.DimensionId, req.ProductId, req.Value,
            req.ManualResult, req.IsFinal, req.FinalOpId, req.Note, UserId));
        return result.IsSuccess
            ? Ok(ApiResponse<MeasureValueDto>.Ok(result.Value))
            : BadRequest(ApiResponse<MeasureValueDto>.Fail(result.Errors));
    }
}

public record SaveMeasureRequest(
    long DimensionId, int ProductId,
    decimal? Value,
    bool? ManualResult,    // Dùng cho text dimension
    bool IsFinal = false,  // true khi re-inspect sau rework (FAI Final)
    int? FinalOpId = null,
    string? Note = null);
