namespace UUcars.API.Services.Cache;

public interface ICacheService
{
    // 核心方法：有缓存直接返回，没有则执行 factory 并缓存结果
    // factory：一个异步函数，当缓存未命中时调用，返回要缓存的数据
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default);

    // 主动删除缓存（数据变更时调用）
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    // 按前缀批量删除（一个列表可能有很多分页缓存，一次性清掉）
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}