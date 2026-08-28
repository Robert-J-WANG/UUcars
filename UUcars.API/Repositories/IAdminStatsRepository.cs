using UUcars.API.DTOs.Responses;

namespace UUcars.API.Repositories;

public interface IAdminStatsRepository
{
    // ── 核心指标 ─────────────────────────────────────────────
    Task<int> GetTotalCarsAsync(CancellationToken cancellationToken = default);
    Task<int> GetPendingCarsAsync(CancellationToken cancellationToken = default);
    Task<int> GetPublishedCarsAsync(CancellationToken cancellationToken = default);
    Task<int> GetTotalUsersAsync(CancellationToken cancellationToken = default);
    Task<int> GetMonthlyOrdersAsync(DateTime startOfMonth, CancellationToken cancellationToken = default);
    Task<decimal> GetMonthlyRevenueAsync(DateTime startOfMonth, CancellationToken cancellationToken = default);

    // ── 趋势数据 ─────────────────────────────────────────────
    Task<List<DailyCountDto>> GetDailyNewCarsAsync(DateTime dailyFrom, DateTime dailyTo,
        CancellationToken cancellationToken = default);

    Task<List<DailyRevenueDto>> GetDailyRevenueAsync(DateTime dailyFrom, DateTime dailyTo,
        CancellationToken cancellationToken = default);

    // ── 品牌分布 ─────────────────────────────────────────────
    Task<List<BrandCountDto>> GetBrandDistributionAsync(int top, CancellationToken cancellationToken = default);
}