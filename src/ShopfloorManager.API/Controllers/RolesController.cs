using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopfloorManager.API.Common;
using ShopfloorManager.Application.Roles;

namespace ShopfloorManager.API.Controllers;

[ApiController]
[Route("api/v1/roles")]
[Authorize]
public class RolesController(IMediator mediator) : ControllerBase
{
    /// <summary>Lấy danh sách roles.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<RoleDto>>), 200)]
    public async Task<IActionResult> GetRoles()
    {
        var result = await mediator.Send(new GetRolesQuery());
        return Ok(ApiResponse<List<RoleDto>>.Ok(result.Value));
    }

    /// <summary>Tạo role mới.</summary>
    [HttpPost]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), 201)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleCommand command)
    {
        var result = await mediator.Send(command);
        return result.IsSuccess
            ? StatusCode(201, ApiResponse<RoleDto>.Ok(result.Value))
            : BadRequest(ApiResponse<RoleDto>.Fail(result.Errors));
    }

    /// <summary>Cập nhật role.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), 200)]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateRoleCommand command)
    {
        var result = await mediator.Send(command with { Id = id });
        return result.IsSuccess
            ? Ok(ApiResponse<RoleDto>.Ok(result.Value))
            : NotFound(ApiResponse<RoleDto>.Fail(result.Errors));
    }
}
