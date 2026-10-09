using StackExchange.Redis;
using UUcars.API.Services.Cache;

namespace UUcars.API.Extensions;

public static class CacheExtensions
{
    public static IServiceCollection AddCacheServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisConnectionString = configuration
            .GetConnectionString("Redis");

        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'Redis' is not configured.");
        }

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "uucars:";
        });

        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }
}
