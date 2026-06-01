namespace UUcars.API.Services.Cache;

// 所有缓存 Key 的集中定义
// 好处：
//   1. IDE 自动补全，不会拼错
//   2. 修改 Key 格式时只改一处
//   3. RemoveByPrefix 用的前缀和 GetOrSet 用的 Key 保证一致
public static class CacheKeys
{
    // 公开车辆列表的前缀（用于批量删除：车辆审核通过时清掉所有分页缓存）
    public const string PublishedCarsPrefix = "cars:published";

    // 待审核车辆列表的前缀（用于批量删除：Admin 用）
    public const string PendingCarsPrefix = "car:pending";

    // 公开车辆列表（带过滤参数）
    // 参数变化 → Key 变化 → 各自独立缓存
    public static string PublishedCars(int page, int pageSize, string? brand, decimal? minPrice, decimal? maxPrice,
        int? minYear, int? maxYear)
    {
        return $"cars:published:p{page}:s{pageSize}" + $":brand{brand ?? ""}:min{minPrice}:max{maxPrice}" +
               $":miny{minYear}:maxy{maxYear}";
    }

    // 待审核车辆列表（Admin 用）
    public static string PendingCars(int page, int pageSize)
    {
        return $"car:pending:p{page}:s{pageSize}";
    }
}