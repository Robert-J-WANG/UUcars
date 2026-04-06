using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UUcars.API.DTOs;
using UUcars.API.DTOs.Requests;
using UUcars.API.DTOs.Responses;
using UUcars.API.Services;

namespace UUcars.API.Controllers;

[ApiController]
[Route("user")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;
    private readonly CurrentUserService _currentUserService;

    public UsersController(UserService userService, CurrentUserService currentUserService)
    {
        _userService = userService;
        _currentUserService = currentUserService;
    }

    // GET /users/me
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetCurrentUserId();

        // 理论上不会发生（[Authorize] 已经保证了 Token 合法），
        // 但做防御性编程，Token 里没有 sub 时返回 401
        if (userId == null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

        var user = await _userService.GetCurrentUserAsync(userId.Value, cancellationToken);
        return Ok(ApiResponse<UserResponse>.Ok(user));
    }

    // PUT /users/me
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var userId= _currentUserService.GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<UserResponse>.Fail("Invalid token."));
        }
        var user = await _userService.UpdateCurrentUserAsync(userId.Value, request, cancellationToken);
        return Ok(ApiResponse<UserResponse>.Ok(user,"Profile updated successfully!"));
    }
}