using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopfloorManager.API.Common;
using ShopfloorManager.Application.Users;
using ShopfloorManager.Shared.Pagination;

namespace ShopfloorManager.API.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public class UsersController(IMediator mediator) : ControllerBase
{
    /// <summary>Lấy danh sách người dùng (phân trang).</summary>
    [HttpGet]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserDto>>), 200)]
    public async Task<IActionResult> GetUsers([FromQuery] GetUsersQuery query)
    {
        var result = await mediator.Send(query);
        if (result.IsFailed) return BadRequest(ApiResponse<UserDto>.Fail(result.Errors));

        var paged = result.Value;
        return Ok(new ApiResponse<IReadOnlyList<UserDto>>
        {
            Success = true,
            Data = paged.Items,
            Pagination = new PaginationMeta(paged.Page, paged.PageSize, paged.Total, paged.TotalPages)
        });
    }

    /// <summary>Lấy thông tin một người dùng.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetUser(int id)
    {
        var result = await mediator.Send(new GetUserByIdQuery(id));
        return result.IsSuccess
            ? Ok(ApiResponse<UserDto>.Ok(result.Value))
            : NotFound(ApiResponse<UserDto>.Fail(result.Errors));
    }

    /// <summary>Tạo người dùng mới.</summary>
    [HttpPost]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command)
    {
        var result = await mediator.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetUser), new { id = result.Value.Id }, ApiResponse<UserDto>.Ok(result.Value))
            : BadRequest(ApiResponse<UserDto>.Fail(result.Errors));
    }

    /// <summary>Cập nhật thông tin người dùng.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserCommand command)
    {
        var result = await mediator.Send(command with { Id = id });
        return result.IsSuccess
            ? Ok(ApiResponse<UserDto>.Ok(result.Value))
            : NotFound(ApiResponse<UserDto>.Fail(result.Errors));
    }

    /// <summary>Đổi mật khẩu (user tự đổi mật khẩu của mình).</summary>
    [HttpPost("me/change-password")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? "0");

        var result = await mediator.Send(new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword));
        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(null!))
            : BadRequest(ApiResponse<object>.Fail(result.Errors));
    }
}

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
