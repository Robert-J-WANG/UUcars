using UUcars.API.DTOs.Responses;
using UUcars.API.Repositories;

namespace UUcars.Tests.Fakes;

public class FakeAdminStatsRepository : IAdminStatsRepository
{
    public AdminStatsResponse Stats { get; set; } = new();

    // ── IAdminStatsRepository 实现 ─────────────────────────────
    public Task<int> GetTotalCarsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Stats.TotalCars);
    }

    public Task<int> GetPendingCarsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Stats.PendingCars);
    }

    public Task<int> GetPublishedCarsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Stats.PublishedCars);
    }

    public Task<int> GetTotalUsersAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Stats.TotalUsers);
    }

    public Task<int> GetMonthlyOrdersAsync(DateTime startOfMonth, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Stats.MonthlyOrders);
    }

    public Task<decimal> GetMonthlyRevenueAsync(DateTime startOfMonth,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Stats.MonthlyRevenue);
    }

    public Task<List<DailyCountDto>> GetDailyNewCarsAsync(DateTime dailyFrom, DateTime dailyTo,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Stats.DailyNewCars);
    }

    public Task<List<DailyRevenueDto>> GetDailyRevenueAsync(DateTime dailyFrom, DateTime dailyTo,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Stats.DailyRevenue);
    }

    public Task<List<BrandCountDto>> GetBrandDistributionAsync(int top,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Stats.BrandDistribution);
    }
}