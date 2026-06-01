using UUcars.API.Services.Cache;

namespace UUcars.Tests.Fakes;

// 测试用的 CacheService：直接执行 factory，不做任何缓存
// 保证单元测试不依赖 Redis，行为和真实 CacheService 一致（逻辑透明）
public class FakeCacheService : ICacheService
{
    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        // 直接执行 factory，不缓存
        return await factory();
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}