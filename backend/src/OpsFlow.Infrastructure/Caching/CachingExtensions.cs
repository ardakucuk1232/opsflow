using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpsFlow.Application.Common.Caching;
using StackExchange.Redis;

namespace OpsFlow.Infrastructure.Caching;

public static class CachingExtensions
{
    public const string RedisConnectionStringName = "Redis";

    public static bool IsRedisConfigured(IConfiguration configuration) =>
        !string.IsNullOrWhiteSpace(configuration.GetConnectionString(RedisConnectionStringName));

    internal static void AddCaching(IServiceCollection services, IConfiguration configuration)
    {
        var redis = configuration.GetConnectionString(RedisConnectionStringName);

        services.AddMemoryCache();
        services.AddSingleton<IAppCache, AppCache>();

        if (string.IsNullOrWhiteSpace(redis))
        {
            services.AddSingleton<ICacheBackend, MemoryCacheBackend>();
            return;
        }

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(redis);

            options.AbortOnConnectFail = false;
            options.BacklogPolicy = BacklogPolicy.FailFast;

            return ConnectionMultiplexer.Connect(options);
        });

        services.AddSingleton<ICacheBackend, RedisCacheBackend>();
    }
}
