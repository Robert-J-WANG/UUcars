using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace UUcars.API.Services.Cache;

public class RedisCacheService : ICacheService
{
    // JSON 序列化选项：属性名用 camelCase，和前端保持一致
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IDistributedCache _cache; // 高层接口：Get/Set/Remove
    private readonly ILogger<RedisCacheService> _logger;
    private readonly IConnectionMultiplexer _redis; // 底层原生：批量删除

    public RedisCacheService(
        IDistributedCache cache,
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _redis = redis;
        _logger = logger;
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        // ── 第一步：尝试从 Redis 读取 ─────────────────────────────────────
        try
        {
            var cached = await _cache.GetStringAsync(key, cancellationToken);
            //                        ↑
            //            IDistributedCache 的扩展方法
            //            内部：GetAsync(key) 返回 byte[]，再转成 string

            if (cached is not null)
            {
                // 命中：把 JSON 字符串反序列化成 T 对象，直接返回
                // 不查数据库，不调用 factory
                _logger.LogDebug("Cache HIT: {Key}", key);
                return JsonSerializer.Deserialize<T>(cached, JsonOptions)!;
            }
        }
        catch (Exception ex)
        {
            // Redis 连不上：不抛异常，继续往下走（降级到查数据库）
            // 这是关键的容错设计：缓存挂了，系统仍然可用
            _logger.LogWarning(ex, "Cache GET failed for key: {Key}", key);
        }

        // ── 第二步：缓存未命中，调用 factory 查数据库 ─────────────────────
        // factory 就是调用方传进来的那个 lambda
        // 只有走到这里才会执行，缓存命中时 factory 根本不会被调用
        _logger.LogDebug("Cache MISS: {Key}", key);
        var data = await factory();
        //                 ↑
        //         执行调用方传入的函数
        //         例：async () => await _carRepository.GetPagedAsync(...)

        // ── 第三步：把结果写入 Redis ──────────────────────────────────────
        try
        {
            var serialized = JsonSerializer.Serialize(data, JsonOptions);
            //  把 T 对象序列化成 JSON 字符串

            await _cache.SetStringAsync(
                key,
                serialized,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ttl
                    // AbsoluteExpiration：从现在起 ttl 时间后过期
                    // 还有 SlidingExpiration：每次访问后重置过期时间
                    // 我们用 Absolute，更可预测
                },
                cancellationToken);
            // 写缓存失败不影响返回结果
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache SET failed for key: {Key}", key);
        }

        return data; // 返回从数据库查到的数据
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Cache REMOVED: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache REMOVE failed for key: {Key}", key);
        }
    }

    public async Task RemoveByPrefixAsync(
        string prefix,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // IDistributedCache 没有按前缀删除的功能
            // 必须用 IConnectionMultiplexer 的原生 API

            // GetServer：获取 Redis 服务器实例（用于执行管理命令）
            var server = _redis.GetServer(_redis.GetEndPoints().First());

            // Keys()：执行 Redis 的 SCAN 命令（生产安全，不用 KEYS）
            // pattern 要加上 InstanceName 前缀，因为 Redis 里实际的 Key 带前缀
            var keys = server.Keys(pattern: $"uucars:{prefix}*").ToArray();
            //                                ↑
            //                         和 InstanceName 保持一致

            if (keys.Length == 0) return;

            // KeyDeleteAsync：批量删除，一次网络往返删多个 Key
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(keys);

            _logger.LogDebug(
                "Cache REMOVED {Count} keys with prefix: {Prefix}",
                keys.Length, prefix);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Cache REMOVE_BY_PREFIX failed for prefix: {Prefix}", prefix);
        }
    }
}