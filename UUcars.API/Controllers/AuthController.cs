using Microsoft.AspNetCore.Mvc;
using UUcars.API.DTOs;
using UUcars.API.DTOs.Requests;
using UUcars.API.DTOs.Responses;
using UUcars.API.Services;

namespace UUcars.API.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly UserService _userService;

    public AuthController(UserService userService)
    {
        _userService = userService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userService.RegisterAsync(request, cancellationToken);

        // 201 Created：表示成功创建了新资源
        // 用 ApiResponse<T>.Ok 包装成统一格式

        // v2:更新注册提示文字
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<UserResponse>.Ok(user,
                "Registration successful. Please check your email to verify your account."));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.LoginAsync(request, cancellationToken);

        // 登录成功返回 200 OK（不是 201，登录不是"创建资源"）
        return Ok(ApiResponse<LoginResponse>.Ok(result, "Login successful."));
    }

    // GET /auth/verify-email?token=xxx
    // 为什么用 GET 而不是 POST？
    // 验证链接直接放在邮件里，用户点击就是一个 GET 请求
    // 浏览器打开链接 → 前端页面从 URL 读取 token → 调用这个接口
    // 如果用 POST，用户点邮件链接后需要前端页面额外触发一次 POST 请求，
    // 但 GET 更简单：前端页面加载时直接把 URL 里的 token 传给这个接口即可
    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail(
        [FromQuery] VerifyEmailRequest request,
        CancellationToken cancellationToken)
    {
        await _userService.VerifyEmailAsync(request.Token, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null!,
            "Email verified successfully. You can now log in."));
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification(
        [FromBody] ResendVerificationRequest request,
        CancellationToken cancellationToken)
    {
        await _userService.ResendVerificationAsync(request.Email, cancellationToken);

        // 无论用户是否存在，始终返回相同的成功响应
        // 不让外部通过响应差异判断邮箱是否注册
        return Ok(ApiResponse<object>.Ok(null!,
            "If this email is registered and unverified, " +
            "a new verification email has been sent."));
    }
}