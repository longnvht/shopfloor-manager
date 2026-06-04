using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopfloorManager.API.Common;
using ShopfloorManager.Application.Dashboard;

namespace ShopfloorManager.API.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public class DashboardController(IMediator mediator) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> Overview()
    {
        var result = await mediator.Send(new GetDashboardOverviewQuery());
        return Ok(ApiResponse<DashboardOverviewDto>.Ok(result.Value));
    }

    [HttpGet("production")]
    public async Task<IActionResult> Production()
    {
        var result = await mediator.Send(new GetProductionKpiQuery());
        return Ok(ApiResponse<ProductionKpiDto>.Ok(result.Value));
    }

    [HttpGet("quality")]
    public async Task<IActionResult> Quality([FromQuery] int days = 30)
    {
        var result = await mediator.Send(new GetQualityKpiQuery(days));
        return Ok(ApiResponse<QualityKpiDto>.Ok(result.Value));
    }
}
