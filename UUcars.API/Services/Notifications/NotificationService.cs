using Microsoft.AspNetCore.SignalR;
using UUcars.API.DTOs.Responses;
using UUcars.API.Entities;
using UUcars.API.Hubs;
using UUcars.API.Repositories;

namespace UUcars.API.Services.Notifications;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repository;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(INotificationRepository repository, IHubContext<NotificationHub> hubContext,
        ILogger<NotificationService> logger)
    {
        _repository = repository;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendNotificationAsync(
        int userId, string type, string message,
        int? relatedId = null, CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Message = message,
            RelatedId = relatedId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(notification, cancellationToken);

        var response = new NotificationResponse
        {
            Id = notification.Id,
            Type = notification.Type,
            Message = notification.Message,
            RelatedId = notification.RelatedId,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt
        };

        // Persist first so an offline user can retrieve the notification later.
        // Real-time delivery is best-effort and must not fail the business action.
        try
        {
            await _hubContext.Clients
                .Group(NotificationGroups.ForUser(userId))
                .SendAsync("ReceiveNotification", response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to push SignalR notification to user {UserId}", userId);
        }
    }

    public async Task<List<NotificationResponse>> GetUnreadNotificationsAsync(
        int userId, CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetAllUnreadAsync(userId, cancellationToken);

        return list.Select(n => new NotificationResponse
        {
            Id = n.Id,
            Type = n.Type,
            Message = n.Message,
            RelatedId = n.RelatedId,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt
        }).ToList();
    }

    public async Task MarkNotificationAsReadAsync(
        int notificationId, int userId, CancellationToken cancellationToken = default)
    {
        var notification = await _repository.GetByIdUnreadAsync(
            notificationId, userId, cancellationToken);

        if (notification == null) return; // 静默处理，不暴露是否存在

        notification.IsRead = true;
        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllNotificationsAsReadAsync(
        int userId, CancellationToken cancellationToken = default)
    {
        await _repository.MarkAllAsReadAsync(userId, cancellationToken);
    }
}
