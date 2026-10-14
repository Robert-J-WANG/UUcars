using UUcars.API.DTOs.Responses;

namespace UUcars.API.Services.Notifications;

public interface INotificationService
{
    /// <summary>
    /// 写入数据库 + 实时推送给目标用户
    /// </summary>
    Task SendNotificationAsync(
        int userId,
        string type,
        string message,
        int? relatedId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询当前用户的未读通知
    /// </summary>
    Task<List<NotificationResponse>> GetUnreadNotificationsAsync(
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记单条已读
    /// </summary>
    Task MarkNotificationAsReadAsync(
        int notificationId,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 全部标记已读
    /// </summary>
    Task MarkAllNotificationsAsReadAsync(
        int userId,
        CancellationToken cancellationToken = default);
}