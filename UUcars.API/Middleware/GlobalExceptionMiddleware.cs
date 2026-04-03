using System.Text.Json;
using UUcars.API.DTOs;
using UUcars.API.Exceptions;

namespace UUcars.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            // 业务异常：预期内的情况，记录 Warning 即可
            _logger.LogWarning("Business exception [{StatusCode}]: {Message}",
                ex.StatusCode, ex.Message);
            await WriteErrorResponseAsync(context, ex.StatusCode, ex.Message);
        }
        catch (Exception ex)
        {
            // 未预期的异常：记录完整堆栈，返回 500
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);
            await WriteErrorResponseAsync(context, StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.");
        }
    }

    private static async Task WriteErrorResponseAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Fail(message);

        // 使用 camelCase，和 Controller 正常返回的 JSON 格式保持一致
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}