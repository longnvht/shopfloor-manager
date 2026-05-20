using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopfloorManager.API.Common;
using ShopfloorManager.Application.Departments;

namespace ShopfloorManager.API.Controllers;

[ApiController]
[Route("api/v1/departments")]
[Authorize]
public class DepartmentsController(IMediator mediator) : ControllerBase
{
    /// <summary>Lấy danh sách phòng ban.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<DepartmentDto>>), 200)]
    public async Task<IActionResult> GetDepartments()
    {
        var result = await mediator.Send(new GetDepartmentsQuery());
        return Ok(ApiResponse<List<DepartmentDto>>.Ok(result.Value));
    }

    /// <summary>Tạo phòng ban mới.</summary>
    [HttpPost]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), 201)]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentCommand command)
    {
        var result = await mediator.Send(command);
        return result.IsSuccess
            ? StatusCode(201, ApiResponse<DepartmentDto>.Ok(result.Value))
            : BadRequest(ApiResponse<DepartmentDto>.Fail(result.Errors));
    }

    /// <summary>Cập nhật phòng ban.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), 200)]
    public async Task<IActionResult> UpdateDepartment(int id, [FromBody] UpdateDepartmentCommand command)
    {
        var result = await mediator.Send(command with { Id = id });
        return result.IsSuccess
            ? Ok(ApiResponse<DepartmentDto>.Ok(result.Value))
            : NotFound(ApiResponse<DepartmentDto>.Fail(result.Errors));
    }
}
