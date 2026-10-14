using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);

    Task<List<Notification>> GetAllUnreadAsync(int userId, CancellationToken cancellationToken = default);

    Task<Notification?> GetByIdUnreadAsync(
        int notificationId, int userId, CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}