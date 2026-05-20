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

    /// <summary>Gửi mã đặt lại mật khẩu về email.</summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
    {
        await mediator.Send(command);
        // Luôn trả 200 để không tiết lộ email có tồn tại không
        return Ok(ApiResponse<object>.Ok(null!));
    }

    /// <summary>Đặt lại mật khẩu bằng mã nhận qua email.</summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        var result = await mediator.Send(command);
        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(null!))
            : BadRequest(ApiResponse<object>.Fail(result.Errors));
    }
}
