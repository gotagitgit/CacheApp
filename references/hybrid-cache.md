# Hybrid Cache Reference

This document explains how CacheApp configures product caching with an in-process L1 cache, Redis L2, and Redis Pub/Sub invalidation. It is a code-oriented reference; the accepted design decision is recorded in [ADR-001](../docs/decisions/ADR-001-hybridcache-and-redis.md).

## Components

| Component | Role | Registration or implementation |
|---|---|---|
| `MemoryCache` | L1 cache local to one API process | `AddMemoryCache` in `InfrastructureServiceCollectionExtensions` |
| Redis distributed cache | Shared L2 cache used across API processes | `AddStackExchangeRedisCache` in `InfrastructureServiceCollectionExtensions` |
| `HybridCache` | Coordinates local and distributed cache operations | `AddHybridCache` in `InfrastructureServiceCollectionExtensions` |
| `HybridProductCache` | Defines product/list cache entries, tags, and expiration options | `GetOrCreateAsync` calls in `HybridProductCache` |
| Redis Pub/Sub | Sends invalidation notifications between API processes | `ProductCacheInvalidator` publishes; `ProductCacheInvalidationSubscriber` receives |

## Registration and L1/L2 flow

`InfrastructureServiceCollectionExtensions.AddInfrastructureAsync` creates one Redis `ConnectionMultiplexer`, registers it as `IConnectionMultiplexer`, then registers both cache providers:

```csharp
services.AddMemoryCache(options => options.SizeLimit = cacheSettings.LocalSizeLimit);
services.AddStackExchangeRedisCache(options =>
{
    options.InstanceName = cacheSettings.RedisInstanceName;
    options.ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(redisMultiplexer);
});
services.AddHybridCache();
```

`AddStackExchangeRedisCache` provides the `IDistributedCache` implementation backed by Redis. `AddHybridCache` registers HybridCache, which uses its in-process cache as L1 and the registered distributed cache as L2. `HybridProductCache` makes reads through `HybridCache.GetOrCreateAsync`.

For a read, HybridCache can return an L1 value immediately. If L1 misses, it checks Redis L2. If both miss, it runs the supplied factory, which loads from the repository/SQL Server, and stores the result in the cache layers. Concurrent misses for the same key are coalesced within one process; there is no distributed lock across processes.

L1 is private to each API process. Redis L2 is shared by API processes configured with the same Redis endpoint and cache prefix. The configured defaults are 10 seconds for local expiration, 30 seconds for distributed expiration, and a local size limit of 20 units. These settings are in `CacheSettings` and can be overridden through configuration.

## Cache keys and tags

`ProductCacheKeys` defines HybridCache identifiers. They are not Redis Pub/Sub topic names.

| Identifier | Example | Applied to | Purpose |
|---|---|---|---|
| `ForItem(id)` | `products:item:6f9619ff-8b86-d011-b42d-00cf4fc964ff` | One product detail entry | Directly address a product entry for lookup/removal |
| `ForList(page, pageSize)` | `products:list:1:10` | One paginated product list | Distinguish page and page-size combinations |
| `ProductTag` | `products` | Product detail and list entries | Broad invalidation, including after Redis reconnect |
| `ProductListTag` | `products:list` | Product list entries | Invalidate all cached list pages after a product write |
| `ForItemTag(id)` | `products:item:6f9619ff-8b86-d011-b42d-00cf4fc964ff` | Detail entry for a product | Tag detail entry by product ID |

The item key and item tag currently have the same string format. Keys identify entries directly; tags group entries so `RemoveByTagAsync` can invalidate a group.

## Write and invalidation flow

After a product is written, `ProductCacheInvalidator` performs local HybridCache invalidation for the item key and product-list tag, then publishes a JSON `ProductCacheInvalidationMessage` on the configured Redis Pub/Sub topic (`Cache:InvalidationChannel`). The message contains schema version, product ID, and the publishing process's instance ID.

Each other API process subscribed to that Redis topic validates the message and ignores its own messages. It queues valid work into its local `Channel<SubscriberWork>`. The background worker removes the item entry and product-list tag through HybridCache. This informs HybridCache to invalidate entries in the local cache and distributed cache as supported by the configured provider, while Pub/Sub ensures other processes also clear their local L1 entries.

```text
Product write
  └─ ProductCacheInvalidator
       ├─ remove item key and product-list tag through HybridCache
       └─ publish product ID on Redis Pub/Sub topic
            └─ each other process receives message
                 └─ local .NET work channel
                      └─ background worker removes item key and list tag
```

The Redis Pub/Sub topic is separate from cache keys/tags. Pub/Sub distributes invalidation events; HybridCache keys and tags identify data to remove.

## Redis reconnect handling

On Redis connection restoration, `ProductCacheInvalidationSubscriber` marks `CacheReadiness` unavailable and queues a reset operation. The worker re-subscribes to the invalidation topic, removes the broad `products` tag, and then marks readiness available. Product read operations wait on `CacheReadiness` before accessing the cache, preventing them from proceeding until this reset path completes.

Pub/Sub messages are not durable. A process disconnected from Redis can miss an invalidation message. The reconnect reset clears product cache entries for that process before reads resume. Cache TTLs also bound how long an entry can remain in Redis, but a write made while Redis invalidation itself fails may leave stale data until expiration.

## Source map

- Registration and provider wiring: `src/CacheApp.Infrastructure/InfrastructureServiceCollectionExtensions.cs`
- Cache keys and tags: `src/CacheApp.Infrastructure/Caching/ProductCacheKeys.cs`
- Product cache reads/options: `src/CacheApp.Infrastructure/Caching/HybridProductCache.cs`
- Write-side invalidation and publish: `src/CacheApp.Infrastructure/Caching/ProductCacheInvalidator.cs`
- Pub/Sub subscription, work queue, and reconnect reset: `src/CacheApp.Infrastructure/Caching/ProductCacheInvalidationSubscriber.cs`
- Readiness state: `src/CacheApp.Infrastructure/Configuration/CacheReadiness.cs`
- Cache settings: `src/CacheApp.Infrastructure/Configuration/CacheSettings.cs`
- Design rationale: `docs/decisions/ADR-001-hybridcache-and-redis.md`
