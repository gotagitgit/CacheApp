using CacheApp.Application.Abstractions;
using CacheApp.Infrastructure.Caching;
using CacheApp.Infrastructure.Configuration;
using CacheApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace CacheApp.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static async Task AddInfrastructureAsync(this IServiceCollection services, IConfiguration configuration)
    {
        var productConnectionString = configuration.GetConnectionString("ProductDatabase");
        if (string.IsNullOrWhiteSpace(productConnectionString))
        {
            throw new InvalidOperationException("Configure ConnectionStrings:ProductDatabase using user secrets or environment configuration.");
        }

        var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379,abortConnect=true";
        var redisConfiguration = ConfigurationOptions.Parse(redisConnectionString);
        var redisMultiplexer = await ConnectionMultiplexer.ConnectAsync(redisConfiguration);
        var cacheSettings = configuration.GetSection("Cache").Get<CacheSettings>() ?? new CacheSettings();

        services.AddOptions<CacheSettings>()
            .Bind(configuration.GetSection("Cache"))
            .Validate(settings => settings.DistributedTtlSeconds > 0, "Distributed TTL must be positive.")
            .Validate(settings => settings.LocalTtlSeconds > 0 && settings.LocalTtlSeconds <= settings.DistributedTtlSeconds, "Local TTL must be positive and no longer than distributed TTL.")
            .Validate(settings => settings.LocalSizeLimit > 0, "Local cache size limit must be positive.")
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.InvalidationChannel), "Invalidation channel is required.")
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.RedisInstanceName), "Redis instance name is required.")
            .ValidateOnStart();

        services.AddSingleton(cacheSettings);
        services.AddSingleton<IConnectionMultiplexer>(redisMultiplexer);
        services.AddDbContext<ProductDbContext>(options => options.UseSqlServer(productConnectionString));
        services.AddMemoryCache(options => options.SizeLimit = cacheSettings.LocalSizeLimit);
        services.AddStackExchangeRedisCache(options =>
        {
            options.InstanceName = cacheSettings.RedisInstanceName;
            options.ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(redisMultiplexer);
        });
        services.AddHybridCache();
        services.AddSingleton<CacheReadiness>();
        services.AddSingleton<ICacheReadiness>(provider => provider.GetRequiredService<CacheReadiness>());
        services.AddSingleton<ProductCacheInstanceId>();
        services.AddSingleton<IProductCache, HybridProductCache>();
        services.AddSingleton<IProductCacheInvalidator, ProductCacheInvalidator>();
        services.AddScoped<IProductRepository, EfProductRepository>();
        services.AddScoped<IReadinessProbe, InfrastructureReadinessProbe>();
        services.AddSingleton<IHostedService, ProductCacheInvalidationSubscriber>();
    }
}
