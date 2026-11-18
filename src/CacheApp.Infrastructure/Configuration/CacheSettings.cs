namespace CacheApp.Infrastructure.Configuration;

public sealed class CacheSettings
{
    public int DistributedTtlSeconds { get; set; } = 30;

    public int LocalTtlSeconds { get; set; } = 10;

    public int LocalSizeLimit { get; set; } = 20;

    public string InvalidationChannel { get; set; } = "product-cache-invalidation";

    public string RedisInstanceName { get; set; } = "product-cache-demo:";

    public TimeSpan DistributedExpiration => TimeSpan.FromSeconds(DistributedTtlSeconds);

    public TimeSpan LocalExpiration => TimeSpan.FromSeconds(LocalTtlSeconds);
}
