using UUcars.API.DTOs.Responses;
using UUcars.API.Repositories;
using UUcars.API.Services.Cache;

namespace UUcars.API.Services;

public class AdminStatsService
{
    private readonly IAdminStatsRepository _adminStatsRepository;
    private readonly TimeProvider _timeProvider;
    private readonly ICacheService _cache;

    public AdminStatsService(IAdminStatsRepository adminStatsRepository, TimeProvider timeProvider, ICacheService cache)
    {
        _adminStatsRepository = adminStatsRepository;
        _timeProvider = timeProvider;
        _cache = cache;
    }

    public async Task<AdminStatsResponse> GetStatsAsync(
        CancellationToken cancellationToken = default)
    {
        // Dashboard 没有分页、筛选或用户私有数据，
        // 因此所有 Admin 共用同一个统计缓存 Key。
        var cacheKey = CacheKeys.AdminStats;

        // 缓存命中：直接返回 Redis 中的 AdminStatsResponse。
        // 缓存未命中：才执行下面 lambda 内的完整统计逻辑。
        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            // 获取当前 UTC 时间。它放在 lambda 内，
            // 只有真的需要重新查询统计数据时才会计算时间边界。
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

            // ── 核心指标 ─────────────────────────────────────────────
            var totalCars = await _adminStatsRepository.GetTotalCarsAsync(cancellationToken);
            var pendingCars = await _adminStatsRepository.GetPendingCarsAsync(cancellationToken);
            var publishedCars = await _adminStatsRepository.GetPublishedCarsAsync(cancellationToken);
            var totalUsers = await _adminStatsRepository.GetTotalUsersAsync(cancellationToken);

            // 当前月份的第一天
            var startOfMonth = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var monthlyOrders = await _adminStatsRepository.GetMonthlyOrdersAsync(startOfMonth, cancellationToken);
            var monthlyRevenue =
                await _adminStatsRepository.GetMonthlyRevenueAsync(startOfMonth, cancellationToken);

            // ── 趋势数据 ─────────────────────────────────────────────
            var todayUtc = DateOnly.FromDateTime(nowUtc);
            // 最近 30 个 UTC 日期：从 29 天前零点开始，
            // 到明天零点前结束。
            var dailyFrom = todayUtc
                .AddDays(-29)
                .ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var dailyTo = todayUtc
                .AddDays(1)
                .ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            var dailyNewCars = await _adminStatsRepository.GetDailyNewCarsAsync(dailyFrom, dailyTo, cancellationToken);
            var dailyRevenue = await _adminStatsRepository.GetDailyRevenueAsync(dailyFrom, dailyTo, cancellationToken);


            // ── 品牌分布 ─────────────────────────────────────────────
            var brandDistribution = await _adminStatsRepository.GetBrandDistributionAsync(10, cancellationToken);

            return new AdminStatsResponse
            {
                TotalCars = totalCars,
                PendingCars = pendingCars,
                PublishedCars = publishedCars,
                TotalUsers = totalUsers,
                MonthlyOrders = monthlyOrders,
                MonthlyRevenue = monthlyRevenue,
                DailyNewCars = FillMissingDailyNewCars(dailyFrom, dailyTo, dailyNewCars),
                DailyRevenue = FillMissingDailyRevenue(dailyFrom, dailyTo, dailyRevenue),
                BrandDistribution = brandDistribution
            };
        }, TimeSpan.FromMinutes(5), cancellationToken);
    }

    // ── 补零辅助方法 ─────────────────────────────────────────────
    private static List<DailyCountDto> FillMissingDailyNewCars(
        DateTime dailyFrom,
        DateTime dailyTo,
        List<DailyCountDto> dailyNewCarsFromDatabase)
    {
        // 将数据库实际返回的“日期 → 新增车辆数”转换为便于按日期查找的结构。
        // Repository 已经按日期 GroupBy，因此每个日期只会有一条记录。
        var countByDate = dailyNewCarsFromDatabase.ToDictionary(
            item => item.Date,
            item => item.Count);

        // 最终返回给前端的完整 30 天序列。
        var completedDailyNewCars = new List<DailyCountDto>();

        // dailyFrom 是包含边界，dailyTo 是不包含边界；
        // 每次循环处理一个 UTC 日期，直到 dailyTo 前一天。
        for (
            var currentDateTime = dailyFrom;
            currentDateTime < dailyTo;
            currentDateTime = currentDateTime.AddDays(1))
        {
            var currentDate = DateOnly.FromDateTime(currentDateTime);

            completedDailyNewCars.Add(new DailyCountDto
            {
                Date = currentDate,

                // 数据库有当天记录时使用真实数量；
                // 没有记录时补 0，确保图表时间轴连续。
                Count = countByDate.GetValueOrDefault(currentDate, 0)
            });
        }

        return completedDailyNewCars;
    }

    private static List<DailyRevenueDto> FillMissingDailyRevenue(
        DateTime dailyFrom,
        DateTime dailyTo,
        List<DailyRevenueDto> dailyRevenueFromDatabase)
    {
        // 将数据库实际返回的“日期 → 当日成交额”转换为按日期查找的结构。
        var revenueByDate = dailyRevenueFromDatabase.ToDictionary(
            item => item.Date,
            item => item.Revenue);

        // 最终返回给前端的完整 30 天序列。
        var completedDailyRevenue = new List<DailyRevenueDto>();

        // 与 Repository 使用相同的 [dailyFrom, dailyTo) UTC 时间范围。
        for (
            var currentDateTime = dailyFrom;
            currentDateTime < dailyTo;
            currentDateTime = currentDateTime.AddDays(1))
        {
            var currentDate = DateOnly.FromDateTime(currentDateTime);

            completedDailyRevenue.Add(new DailyRevenueDto
            {
                Date = currentDate,

                // 数据库没有当天成交订单时，成交额应为 0。
                Revenue = revenueByDate.GetValueOrDefault(currentDate, 0m)
            });
        }

        return completedDailyRevenue;
    }
}