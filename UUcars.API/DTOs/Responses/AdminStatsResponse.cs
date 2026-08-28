namespace UUcars.API.DTOs.Responses;

public class AdminStatsResponse
{
    // 核心指标（统计卡片用）
    public int TotalCars { get; set; }
    public int PendingCars { get; set; }
    public int PublishedCars { get; set; }
    public int TotalUsers { get; set; }
    public int MonthlyOrders { get; set; }
    public decimal MonthlyRevenue { get; set; }

    // 趋势数据 （折线图用）
    public List<DailyCountDto> DailyNewCars { get; set; } = [];
    public List<DailyRevenueDto> DailyRevenue { get; set; } = [];

    // 品牌分布 （饼图用）
    public List<BrandCountDto> BrandDistribution { get; set; } = [];
}

public class DailyCountDto
{
    public DateOnly Date { get; set; }
    public int Count { get; set; }
}

public class DailyRevenueDto
{
    public DateOnly Date { get; set; }
    public decimal Revenue { get; set; }
}

public class BrandCountDto
{
    public string Brand { get; set; } = string.Empty;
    public int Count { get; set; }
}