namespace CacheApp.Infrastructure.Caching;

public sealed class ProductCacheInstanceId
{
    public string Value { get; } = Guid.NewGuid().ToString("N");
}
