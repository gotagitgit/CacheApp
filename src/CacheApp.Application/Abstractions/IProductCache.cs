using CacheApp.Application.Products;

namespace CacheApp.Application.Abstractions;

public interface IProductCache
{
    ValueTask<ProductPageResponse> GetOrCreatePageAsync(
        int page,
        int pageSize,
        Func<CancellationToken, ValueTask<ProductPageResponse>> factory,
        CancellationToken cancellationToken);

    ValueTask<ProductResponse?> GetOrCreateProductAsync(
        Guid id,
        Func<CancellationToken, ValueTask<ProductResponse?>> factory,
        CancellationToken cancellationToken);
}
