using UUcars.API.DTOs.Responses;
using UUcars.API.Services;
using UUcars.Tests.Fakes;

namespace UUcars.Tests.Services;

public class AdminStatsServiceTests
{
    // 和现有 CarServiceTests、OrderServiceTests 保持相同结构：
    // 统一在辅助方法中创建被测试的 Service。
    private static AdminStatsService CreateService(
        FakeAdminStatsRepository repository,
        TimeProvider? timeProvider = null)
    {
        return new AdminStatsService(
            repository,
            timeProvider ?? TimeProvider.System,
            new FakeCacheService());
    }

    [Fact]
    public async Task GetStatsAsync_ShouldFillMissingDaysForLastThirtyDays()
    {
        // Arrange
        var repository = new FakeAdminStatsRepository
        {
            Stats = new AdminStatsResponse
            {
                DailyNewCars =
                [
                    new DailyCountDto
                    {
                        Date = new DateOnly(2026, 7, 30),
                        Count = 2
                    },
                    new DailyCountDto
                    {
                        Date = new DateOnly(2026, 8, 28),
                        Count = 1
                    }
                ],
                DailyRevenue =
                [
                    new DailyRevenueDto
                    {
                        Date = new DateOnly(2026, 8, 27),
                        Revenue = 12000m
                    }
                ]
            }
        };

        // 固定“当前时间”，避免测试结果随着真实日期变化。
        var timeProvider = new FixedTimeProvider(
            new DateTimeOffset(
                2026, 8, 28,
                12, 0, 0,
                TimeSpan.Zero));

        var service = CreateService(repository, timeProvider);


        // Act
        var result = await service.GetStatsAsync();

        // Assert：近 30 天必须正好返回 30 项
        Assert.Equal(30, result.DailyNewCars.Count);
        Assert.Equal(30, result.DailyRevenue.Count);

        // Assert：包含今天，从 29 天前开始
        Assert.Equal(
            new DateOnly(2026, 7, 30),
            result.DailyNewCars[0].Date);

        Assert.Equal(
            new DateOnly(2026, 8, 28),
            result.DailyNewCars[^1].Date);

        // Assert：数据库已有的数据保留原值
        Assert.Equal(
            2,
            result.DailyNewCars.Single(item => item.Date == new DateOnly(2026, 7, 30)).Count);

        Assert.Equal(
            1,
            result.DailyNewCars.Single(item => item.Date == new DateOnly(2026, 8, 28)).Count);

        Assert.Equal(
            12000m,
            result.DailyRevenue.Single(item => item.Date == new DateOnly(2026, 8, 27)).Revenue);

        // Assert：数据库没有返回的日期补 0
        Assert.Equal(
            0,
            result.DailyNewCars.Single(item => item.Date == new DateOnly(2026, 7, 31)).Count);

        Assert.Equal(
            0m,
            result.DailyRevenue.Single(item => item.Date == new DateOnly(2026, 7, 31)).Revenue);
    }
}

// TimeProvider.System 会读取运行测试时的真实时间。
// 这个测试替代实现只返回构造时传入的固定 UTC 时间。
internal class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _utcNow;

    public FixedTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return _utcNow;
    }
}