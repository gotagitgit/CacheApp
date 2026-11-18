namespace CacheApp.Application.Abstractions;

public interface IProductCacheInvalidator
{
    Task InvalidateAfterWriteAsync(Guid productId);
}
