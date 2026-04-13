using Microsoft.EntityFrameworkCore;
using UUcars.API.Data;
using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public class EfFavoriteRepository : IFavoriteRepository
{
    private readonly AppDbContext _context;

    public EfFavoriteRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Favorite?> GetAsync(int userId, int carId,
        CancellationToken cancellationToken = default)
    {
        // Favorites 表用的是联合主键（UserId, CarId）
        // FindAsync 支持联合主键，传入多个值按主键定义的顺序匹配
        return await _context.Favorites
            .FindAsync([userId, carId], cancellationToken);
    }

    public async Task<Favorite> AddAsync(Favorite favorite,
        CancellationToken cancellationToken = default)
    {
        _context.Favorites.Add(favorite);
        await _context.SaveChangesAsync(cancellationToken);
        return favorite;
    }

    public async Task DeleteAsync(Favorite favorite,
        CancellationToken cancellationToken = default)
    {
        _context.Favorites.Remove(favorite);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<(List<Favorite> Favorites, int TotalCount)> GetByUserAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Favorites
            .Include(f => f.Car)
            .ThenInclude(c => c.Seller)  // 加载车辆的同时也加载车辆的卖家
            .Where(f => f.UserId == userId);

        var totalCount = await query.CountAsync(cancellationToken);

        var favorites = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (favorites, totalCount);
    }
}