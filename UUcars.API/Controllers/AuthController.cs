using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using UUcars.API.DTOs;
using UUcars.API.DTOs.Requests;
using UUcars.API.DTOs.Responses;
using UUcars.API.Extensions;
using UUcars.API.Services;

namespace UUcars.API.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly UserService _userService;
    private readonly RefreshTokenService _refreshTokenService;

    public AuthController(UserService userService, RefreshTokenService refreshTokenService)
    {
        _userService = userService;
        _refreshTokenService = refreshTokenService;
    }

    [HttpPost("register")]
    // 使用限流
    // ✅ 注册：每IP每小时5次
    [EnableRateLimiting(RateLimitPolicies.Register)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userService.RegisterAsync(request.Username, request.Email, request.Password,
            cancellationToken);

        // 201 Created：表示成功创建了新资源
        // 用 ApiResponse<T>.Ok 包装成统一格式

        // v2:更新注册提示文字
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<UserResponse>.Ok(user,
                "Registration successful. Please check your email to verify your account."));
    }

    [HttpPost("login")]
    // 使用限流
    // ✅ 登录：每IP每分钟10次
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.LoginAsync(request.Email, request.Password, cancellationToken);

        // ✅ 新增：生成 RefreshToken 并写入 HttpOnly Cookie

        var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(result.User.Id, cancellationToken);
        SetRefreshTokenCookie(refreshToken.Token, refreshToken.ExpiresAt);

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
    [EnableRateLimiting(RateLimitPolicies.ResendVerification)]
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

    // POST /auth/forgot-password
    [HttpPost("forgot-password")]
    // 使用限流
    // ✅ 忘记密码：每IP每小时3次
    [EnableRateLimiting(RateLimitPolicies.ForgotPassword)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _userService.ForgotPasswordAsync(request.Email, cancellationToken);

        // 无论用户是否存在、邮箱是否已验证，始终返回相同的成功响应
        // 保持和 ResendVerification 一致的安全设计
        return Ok(ApiResponse<object>.Ok(null!,
            "If this email is registered, a password reset link has been sent."));
    }

    // POST /auth/reset-password
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _userService.ResetPasswordAsync(request.Token, request.NewPassword, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null!,
            "Password has been reset successfully. You can now log in with your new password."));
    }


    /// <summary>
    /// 用 RefreshToken 换取新的 AccessToken
    /// RefreshToken 从 HttpOnly Cookie 里自动读取，不需要前端手动传
    /// </summary>
    // POST /auth/refresh
    [HttpPost("refresh")]
    [EnableRateLimiting(RateLimitPolicies.Write)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        // 从 Cookie 里读取 RefreshToken
        // 前端不需要传任何 Body，浏览器自动携带 Cookie
        if (!Request.Cookies.TryGetValue("refreshToken", out var token)
            || string.IsNullOrEmpty(token))
            return Unauthorized(ApiResponse<object>.Fail("Refresh token not found"));

        var (newAccessToken, newRefreshToken) = await _refreshTokenService
            .RefreshAsync(token, cancellationToken);

        // 更新 Cookie（新的 RefreshToken 覆盖旧的）
        SetRefreshTokenCookie(newRefreshToken.Token, newRefreshToken.ExpiresAt);

        var response = new RefreshTokenResponse
        {
            AccessToken = newAccessToken
        };

        return Ok(ApiResponse<RefreshTokenResponse>.Ok(response, "token refreshed"));
    }


    /// <summary>
    /// 登出：撤销当前设备的 RefreshToken，清除 Cookie
    /// </summary>
    [HttpPost("logout")]
    [EnableRateLimiting(RateLimitPolicies.Write)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        // 从 Cookie 里读取 RefreshToken
        Request.Cookies.TryGetValue("refreshToken", out var token);

        // 撤销 Token（即使 Token 不存在也静默处理）
        if (!string.IsNullOrEmpty(token)) await _refreshTokenService.RevokeAsync(token, cancellationToken);

        // 清除 Cookie
        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            Path = "/", // 和 SetRefreshTokenCookie 里的 Path 保持一致，否则删不掉
            SameSite = SameSiteMode.None, // 必须和 Set-Cookie 时的属性一致才能删掉
            Secure = true
        });

        return Ok(ApiResponse<object>.Ok("Logged out successfully"));
    }

    /// <summary>
    /// 把 RefreshToken 写入 HttpOnly Cookie
    /// 提取为私有方法：Login 和 Refresh 都需要调用
    /// </summary>
    private void SetRefreshTokenCookie(string token, DateTime expiresAt)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true, // JS 无法读取
            Secure = true, // 只通过 HTTPS 传输
            SameSite = SameSiteMode.None, // 防 CSRF 通过其他配置
            Expires = expiresAt, // 和 RefreshToken 过期时间一致
            Path = "/" // 全站发送，避免非/auth路径下无法携带Cookie
        };

        // 开发环境：Secure=false（本地没有 HTTPS）
        // 通过环境变量判断，避免本地开发时 Cookie 无法设置
        if (HttpContext.RequestServices
            .GetRequiredService<IWebHostEnvironment>()
            .IsDevelopment())
            cookieOptions.Secure = false;

        Response.Cookies.Append("refreshToken", token, cookieOptions);
    }
}