using CacheApp.Application.Abstractions;
using CacheApp.Domain.Products;

namespace CacheApp.Application.Products;

public sealed class ProductService(
    IProductRepository productRepository,
    IProductCache productCache,
    ICacheReadiness cacheReadiness,
    IProductCacheInvalidator cacheInvalidator)
{
    public async Task<ProductPageResponse> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        await cacheReadiness.WaitUntilReadyAsync(cancellationToken);

        return await productCache.GetOrCreatePageAsync(
            page,
            pageSize,
            async token =>
            {
                var data = await productRepository.GetPageAsync(page, pageSize, token);
                var products = data.Products.Select(ToResponse).ToArray();
                return new ProductPageResponse(products, page, pageSize, data.TotalCount);
            },
            cancellationToken);
    }

    public async Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await cacheReadiness.WaitUntilReadyAsync(cancellationToken);

        return await productCache.GetOrCreateProductAsync(
            id,
            async token =>
            {
                var product = await productRepository.GetByIdAsync(id, token);
                return product is null ? null : ToResponse(product);
            },
            cancellationToken);
    }

    public async Task<ProductResponse> CreateAsync(ProductRequest request, CancellationToken cancellationToken)
    {
        var product = new Product(
            Guid.NewGuid(),
            request.Name.Trim(),
            request.Description?.Trim(),
            request.Price,
            request.StockQuantity);

        await productRepository.AddAsync(product, cancellationToken);
        await productRepository.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateAfterWriteAsync(product.Id);

        return ToResponse(product);
    }

    public async Task<ProductResponse?> UpdateAsync(Guid id, ProductRequest request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return null;
        }

        var updatedProduct = new Product(
            product.Id,
            request.Name.Trim(),
            request.Description?.Trim(),
            request.Price,
            request.StockQuantity);

        await productRepository.UpdateAsync(updatedProduct, cancellationToken);
        await cacheInvalidator.InvalidateAfterWriteAsync(id);

        return ToResponse(updatedProduct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetTrackedByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return false;
        }

        productRepository.Remove(product);
        await productRepository.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateAfterWriteAsync(id);
        return true;
    }

    private static ProductResponse ToResponse(Product product) => new(
        product.Id,
        product.Name,
        product.Description,
        product.Price,
        product.StockQuantity);
}
