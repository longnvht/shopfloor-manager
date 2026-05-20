using System.Text.Json;
using FluentValidation;
using ShopfloorManager.API.Common;

namespace ShopfloorManager.API.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (ValidationException ex)
        {
            ctx.Response.StatusCode = 400;
            ctx.Response.ContentType = "application/json";
            var error = string.Join("; ", ex.Errors.Select(e => e.ErrorMessage));
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(ApiResponse<object>.Fail(error), JsonOptions));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            ctx.Response.StatusCode = 500;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(ApiResponse<object>.Fail("Lỗi hệ thống. Vui lòng thử lại."), JsonOptions));
        }
    }
}
