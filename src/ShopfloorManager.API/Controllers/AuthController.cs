using MediatR;
using Microsoft.AspNetCore.Mvc;
using ShopfloorManager.API.Common;
using ShopfloorManager.Application.Auth;

namespace ShopfloorManager.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(IMediator mediator) : ControllerBase
{
    /// <summary>Đăng nhập và nhận JWT token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await mediator.Send(command);
        return result.IsSuccess
            ? Ok(ApiResponse<LoginResponse>.Ok(result.Value))
            : Unauthorized(ApiResponse<LoginResponse>.Fail(result.Errors));
    }
}
