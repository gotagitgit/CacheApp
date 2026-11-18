namespace CacheApp.Infrastructure.Caching;

public sealed record ProductCacheInvalidationMessage(
    int SchemaVersion,
    Guid ProductId,
    string OriginInstanceId);
