using Microsoft.EntityFrameworkCore;
using UUcars.API.Data;
using UUcars.API.DTOs.Responses;
using UUcars.API.Entities.Enums;

namespace UUcars.API.Repositories;

public class EfAdminStatsRepository : IAdminStatsRepository
{
    private readonly AppDbContext _context;

    public EfAdminStatsRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── 核心指标 ─────────────────────────────────────────────
    public Task<int> GetTotalCarsAsync(CancellationToken cancellationToken = default)
    {
        return _context.Cars
            .CountAsync(c => c.Status != CarStatus.Deleted, cancellationToken);
    }

    public Task<int> GetPendingCarsAsync(CancellationToken cancellationToken = default)
    {
        return _context.Cars
            .CountAsync(c => c.Status == CarStatus.PendingReview, cancellationToken);
    }

    public Task<int> GetPublishedCarsAsync(CancellationToken cancellationToken = default)
    {
        return _context.Cars
            .CountAsync(c => c.Status == CarStatus.Published, cancellationToken);
    }

    public Task<int> GetTotalUsersAsync(CancellationToken cancellationToken = default)
    {
        return _context.Users.CountAsync(cancellationToken);
    }

    public Task<int> GetMonthlyOrdersAsync(
        DateTime startOfMonth,
        CancellationToken cancellationToken = default)
    {
        return _context.Orders
            .CountAsync(o => o.CreatedAt >= startOfMonth &&
                             o.CreatedAt < startOfMonth.AddMonths(1), cancellationToken);
    }

    public async Task<decimal> GetMonthlyRevenueAsync(
        DateTime startOfMonth,
        CancellationToken cancellationToken = default)
    {
        // SumAsync 在没有数据时返回 null（因为类型是 decimal?），
        // 所以这里用 ?? 0m 保证返回值不为 null
        var result = await _context.Orders
            .Where(o => o.Status == OrderStatus.Completed && o.UpdatedAt >= startOfMonth &&
                        o.UpdatedAt < startOfMonth.AddMonths(1)).Select(o => (decimal?)o.Price)
            .SumAsync(cancellationToken);

        return result ?? 0m;
    }

    // ── 趋势数据 ─────────────────────────────────────────────

    public async Task<List<DailyCountDto>> GetDailyNewCarsAsync(
        DateTime dailyFrom,
        DateTime dailyTo,
        CancellationToken cancellationToken = default)
    {
        return await _context.Cars
            .Where(c => c.CreatedAt >= dailyFrom && c.CreatedAt < dailyTo)
            .GroupBy(c => c.CreatedAt.Date).Select(g => new DailyCountDto
                {
                    Date = DateOnly.FromDateTime(g.Key),
                    Count = g.Count()
                }
            ).OrderBy(item => item.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DailyRevenueDto>> GetDailyRevenueAsync(
        DateTime dailyFrom,
        DateTime dailyTo,
        CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Where(o => o.Status == OrderStatus.Completed && o.UpdatedAt >= dailyFrom && o.UpdatedAt < dailyTo)
            .GroupBy(o => o.UpdatedAt.Date).Select(g => new DailyRevenueDto
            {
                Date = DateOnly.FromDateTime(g.Key),
                Revenue = g.Sum(o => o.Price)
            }).OrderBy(item => item.Date).ToListAsync(cancellationToken);
    }

    // ── 品牌分布 ─────────────────────────────────────────────
    public Task<List<BrandCountDto>> GetBrandDistributionAsync(
        int top,
        CancellationToken cancellationToken = default)
    {
        return _context.Cars
            .Where(c => c.Status == CarStatus.Published || c.Status == CarStatus.Sold)
            .GroupBy(c => c.Brand)
            .Select(g => new BrandCountDto
            {
                Brand = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Brand)
            .Take(top)
            .ToListAsync(cancellationToken);
    }
}