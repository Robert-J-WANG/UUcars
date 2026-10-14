using Microsoft.EntityFrameworkCore;
using UUcars.API.Data;
using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public class EfNotificationRepository : INotificationRepository
{
    private readonly AppDbContext _context;

    public EfNotificationRepository(AppDbContext context)
    {
        _context = context;
    }

    // 添加一个消息到数据库
    public async Task AddAsync(
        Notification notification, CancellationToken cancellationToken = default)
    {
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);
    }

    // 取出所有未读消息
    public async Task<List<Notification>> GetAllUnreadAsync(
        int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    // 取出一个未读消息
    public async Task<Notification?> GetByIdUnreadAsync(
        int notificationId, int userId, CancellationToken cancellationToken = default)
    {
        // 同时按 notificationId 和 userId 过滤，防止 IDOR
        // （不能只按 id 查到就随便让人标记别人的通知已读）
        return await _context.Notifications
            .FirstOrDefaultAsync(
                n => n.Id == notificationId && n.UserId == userId && !n.IsRead,
                cancellationToken);
    }


    public async Task MarkAllAsReadAsync(
        int userId, CancellationToken cancellationToken = default)
    {
        // ExecuteUpdateAsync：EF Core 7+ 提供的批量更新 API
        // 直接在数据库层面执行一条 UPDATE 语句，不需要先把符合条件的
        // 实体全部加载进内存、逐个改字段再 SaveChanges——未读通知可能有
        // 几十上百条，这种场景下批量更新比"加载 + 逐条改 + 保存"效率高得多
        await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(
                s => s.SetProperty(n => n.IsRead, true),
                cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}