using System.Text.Json;
using CacheApp.Application.Abstractions;
using CacheApp.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CacheApp.Infrastructure.Caching;

public sealed class ProductCacheInvalidator(
    HybridCache hybridCache,
    IConnectionMultiplexer redis,
    IOptions<CacheSettings> cacheSettings,
    ProductCacheInstanceId instanceId,
    ILogger<ProductCacheInvalidator> logger) : IProductCacheInvalidator
{
    public async Task InvalidateAfterWriteAsync(Guid productId)
    {
        await TryInvalidateItemAsync(productId, CancellationToken.None);
        await TryInvalidateListAsync(CancellationToken.None);
        await TryPublishAsync(productId);
    }

    private async Task TryInvalidateItemAsync(Guid productId, CancellationToken cancellationToken)
    {
        try
        {
            await hybridCache.RemoveAsync(ProductCacheKeys.ForItem(productId), cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not invalidate product detail cache for {ProductId}.", productId);
        }
    }

    private async Task TryInvalidateListAsync(CancellationToken cancellationToken)
    {
        try
        {
            await hybridCache.RemoveByTagAsync(ProductCacheKeys.ProductListTag, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not invalidate paginated product list cache.");
        }
    }

    private async Task TryPublishAsync(Guid productId)
    {
        try
        {
            var message = new ProductCacheInvalidationMessage(1, productId, instanceId.Value);
            var payload = JsonSerializer.Serialize(message);
            var channel = RedisChannel.Literal(cacheSettings.Value.InvalidationChannel);
            await redis.GetSubscriber().PublishAsync(channel, payload);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not publish cache invalidation for product {ProductId}.", productId);
        }
    }
}
