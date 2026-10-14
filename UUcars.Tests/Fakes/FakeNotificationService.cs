using UUcars.API.DTOs.Responses;
using UUcars.API.Services.Notifications;

namespace UUcars.Tests.Fakes;

public class FakeNotificationService : INotificationService
{
    public record SentNotification(
        int UserId,
        string Type,
        string Message,
        int? RelatedId);

    public List<SentNotification> SentNotifications { get; } = new();

    public Task SendNotificationAsync
    (
        int userId,
        string type,
        string message,
        int? relatedId = null,
        CancellationToken cancellationToken = default)
    {
        SentNotifications.Add(new SentNotification(userId, type, message, relatedId));
        return Task.CompletedTask;
    }

    public Task<List<NotificationResponse>> GetUnreadNotificationsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new List<NotificationResponse>());
    }

    public Task MarkNotificationAsReadAsync(
        int notificationId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task MarkAllNotificationsAsReadAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}