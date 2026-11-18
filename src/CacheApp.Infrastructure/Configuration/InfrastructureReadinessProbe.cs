using CacheApp.Application.Abstractions;
using CacheApp.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CacheApp.Infrastructure.Configuration;

public sealed class InfrastructureReadinessProbe(
    ProductDbContext database,
    IConnectionMultiplexer redis,
    CacheReadiness cacheReadiness,
    ILogger<InfrastructureReadinessProbe> logger) : IReadinessProbe
{
    public async Task<bool> IsReadyAsync(CancellationToken cancellationToken)
    {
        if (!cacheReadiness.IsReady || !redis.IsConnected)
        {
            return false;
        }

        try
        {
            return await database.Database.CanConnectAsync(cancellationToken)
                && await RedisIsReadyAsync();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "A product API dependency readiness check failed.");
            return false;
        }
    }

    private async Task<bool> RedisIsReadyAsync()
    {
        await redis.GetDatabase().PingAsync();
        return true;
    }
}
