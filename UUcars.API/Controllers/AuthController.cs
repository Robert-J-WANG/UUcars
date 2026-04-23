using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UUcars.API.DTOs;
using UUcars.API.DTOs.Requests;
using UUcars.API.DTOs.Responses;
using UUcars.API.Services;
using UUcars.API.Services.Email;

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
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<UserResponse>.Ok(user, "Registration successful."));
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

    // 临时测试接口：验证 JWT 认证配置是否生效
    // 测试完成后会删掉，Step 12 会建正式的 GET /users/me
    [HttpGet("test-auth")]
    [Authorize]
    // [Authorize(Roles = "User")]
    public IActionResult TestAuth()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;
        // User 是 ControllerBase 提供的属性，类型是 ClaimsPrincipal
        // 认证中间件在验证 Token 后，会把 Token Payload 里的所有 Claims
        // 解析出来注入到这个对象里
        //
        // FindFirst(key)：在 Claims 集合里找第一个匹配 key 的 Claim，返回 Claim 对象
        // ?.Value：取这个 Claim 的值（字符串），用 ?. 是因为找不到时返回 null
        //
        // 为什么要同时找 ClaimTypes.NameIdentifier 和 "sub"？
        // 不同版本的 JWT 库对 "sub" 的处理不一样：
        //   有些版本会把 "sub" 映射成 ClaimTypes.NameIdentifier（一个很长的 URI）
        //   有些版本直接保留 "sub" 这个简短名字
        // 两个都找，确保能取到值

        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        return Ok(ApiResponse<object>.Ok(new { userId, email, role }, "Token is valid."));
    }

    // 临时测试接口，验证邮件服务配置是否正常
// 测试完成后立刻删除
    [HttpPost("test-email")]
    public async Task<IActionResult> TestEmail(
        [FromQuery] string to,
        [FromServices] IEmailService emailService,
        CancellationToken cancellationToken)
    {
        var fakeData = new { name = "John", email = "jion@gmail.com" };
        await emailService.SendPasswordResetAsync(
            to, "test-token-12345", cancellationToken);
        return Ok(ApiResponse<object>.Ok(fakeData, $"Test email sent to {to}"));
    }
}