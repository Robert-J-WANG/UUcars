using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using UUcars.API.DTOs;
using UUcars.API.DTOs.Responses;
using UUcars.API.Extensions;
using UUcars.API.Services;
using UUcars.API.Services.Notifications;

namespace UUcars.API.Controllers;

[ApiController]
[Route("notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly CurrentUserService _currentUserService;

    public NotificationsController(
        INotificationService notificationService,
        CurrentUserService currentUserService)
    {
        _notificationService = notificationService;
        _currentUserService = currentUserService;
    }


    // GET /notifications
    [HttpGet]
    [EnableRateLimiting(RateLimitPolicies.Browse)]
    public async Task<IActionResult> GetAllUnread(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

        var notifications = await _notificationService
            .GetUnreadNotificationsAsync(userId.Value, cancellationToken);

        return Ok(ApiResponse<List<NotificationResponse>>.Ok(notifications));
    }


    // PUT /notifications/{id}/read
    [HttpPut("{id:int}/read")]
    [EnableRateLimiting(RateLimitPolicies.Write)]
    public async Task<IActionResult> MarkAsRead(int id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

        await _notificationService.MarkNotificationAsReadAsync(id, userId.Value, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Notification marked as read."));
    }

    // PUT /notifications/read-all
    [HttpPut("read-all")]
    [EnableRateLimiting(RateLimitPolicies.Write)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

        await _notificationService.MarkAllNotificationsAsReadAsync(userId.Value, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "All notifications marked as read."));
    }
}