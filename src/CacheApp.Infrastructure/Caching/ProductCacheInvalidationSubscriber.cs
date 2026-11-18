using System.Text.Json;
using System.Threading.Channels;
using CacheApp.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CacheApp.Infrastructure.Caching;

public sealed class ProductCacheInvalidationSubscriber(
    HybridCache hybridCache,
    IConnectionMultiplexer redis,
    IOptions<CacheSettings> cacheSettings,
    ProductCacheInstanceId instanceId,
    CacheReadiness readiness,
    ILogger<ProductCacheInvalidationSubscriber> logger) : IHostedService
{
    private readonly Channel<SubscriberWork> _work = Channel.CreateUnbounded<SubscriberWork>();
    private readonly CancellationTokenSource _stopping = new();
    private ISubscriber? _subscriber;
    private Task? _worker;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!redis.IsConnected)
        {
            throw new InvalidOperationException("Redis must be connected before the API starts.");
        }

        _subscriber = redis.GetSubscriber();
        var channel = RedisChannel.Literal(cacheSettings.Value.InvalidationChannel);
        await _subscriber.SubscribeAsync(channel, OnMessage);
        redis.ConnectionRestored += OnConnectionRestored;
        readiness.MarkReady();
        _worker = ProcessWorkAsync(_stopping.Token);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        redis.ConnectionRestored -= OnConnectionRestored;
        _stopping.Cancel();
        _work.Writer.TryComplete();

        if (_subscriber is not null)
        {
            var channel = RedisChannel.Literal(cacheSettings.Value.InvalidationChannel);
            await _subscriber.UnsubscribeAsync(channel, OnMessage);
        }

        if (_worker is not null)
        {
            await _worker.WaitAsync(cancellationToken);
        }
    }

    private void OnMessage(RedisChannel channel, RedisValue value)
    {
        try
        {
            var message = JsonSerializer.Deserialize<ProductCacheInvalidationMessage>((string)value!);
            if (message is null || message.SchemaVersion != 1 || message.OriginInstanceId == instanceId.Value)
            {
                return;
            }

            _work.Writer.TryWrite(new SubscriberWork(message, ResetProductCache: false));
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Ignoring invalid product cache invalidation message.");
        }
    }

    private void OnConnectionRestored(object? sender, ConnectionFailedEventArgs eventArgs)
    {
        readiness.MarkUnavailable();
        _work.Writer.TryWrite(new SubscriberWork(null, ResetProductCache: true));
    }

    private async Task ProcessWorkAsync(CancellationToken cancellationToken)
    {
        await foreach (var work in _work.Reader.ReadAllAsync(cancellationToken))
        {
            if (work.ResetProductCache)
            {
                await RestoreCacheReadinessAsync(cancellationToken);
                continue;
            }

            if (work.Message is not null)
            {
                await InvalidateRemoteEntryAsync(work.Message.ProductId, cancellationToken);
            }
        }
    }

    private async Task InvalidateRemoteEntryAsync(Guid productId, CancellationToken cancellationToken)
    {
        try
        {
            await hybridCache.RemoveAsync(ProductCacheKeys.ForItem(productId), cancellationToken);
            await hybridCache.RemoveByTagAsync(ProductCacheKeys.ProductListTag, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not apply remote cache invalidation for product {ProductId}.", productId);
        }
    }

    private async Task RestoreCacheReadinessAsync(CancellationToken cancellationToken)
    {
        var channel = RedisChannel.Literal(cacheSettings.Value.InvalidationChannel);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_subscriber is not null)
                {
                    await _subscriber.SubscribeAsync(channel, OnMessage);
                }

                await hybridCache.RemoveByTagAsync(ProductCacheKeys.ProductTag, cancellationToken);
                readiness.MarkReady();
                return;
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(exception, "Retrying product cache reset after Redis reconnect.");
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }

    private sealed record SubscriberWork(ProductCacheInvalidationMessage? Message, bool ResetProductCache);
}
