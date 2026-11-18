using CacheApp.Application.Abstractions;
using CacheApp.Application.Products;
using CacheApp.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace CacheApp.Infrastructure.Caching;

public sealed class HybridProductCache(
    HybridCache hybridCache,
    IOptions<CacheSettings> cacheSettings) : IProductCache
{
    public ValueTask<ProductPageResponse> GetOrCreatePageAsync(
        int page,
        int pageSize,
        Func<CancellationToken, ValueTask<ProductPageResponse>> factory,
        CancellationToken cancellationToken) =>
        hybridCache.GetOrCreateAsync(
            ProductCacheKeys.ForList(page, pageSize),
            state: factory,
            factory: static async (loadPage, context, token) =>
            {
                var response = await loadPage(token);
                context.LocalSize = Math.Max(1, response.Items.Count);
                return response;
            },
            CreateCacheOptions(),
            tags: [ProductCacheKeys.ProductTag, ProductCacheKeys.ProductListTag],
            cancellationToken);

    public ValueTask<ProductResponse?> GetOrCreateProductAsync(
        Guid id,
        Func<CancellationToken, ValueTask<ProductResponse?>> factory,
        CancellationToken cancellationToken) =>
        hybridCache.GetOrCreateAsync(
            ProductCacheKeys.ForItem(id),
            state: factory,
            factory: static (loadProduct, _, token) => loadProduct(token),
            CreateCacheOptions(localSize: 1),
            tags: [ProductCacheKeys.ProductTag, ProductCacheKeys.ForItemTag(id)],
            cancellationToken);

    private HybridCacheEntryOptions CreateCacheOptions(long? localSize = null) => new()
    {
        Expiration = cacheSettings.Value.DistributedExpiration,
        LocalCacheExpiration = cacheSettings.Value.LocalExpiration,
        LocalSize = localSize
    };
}
