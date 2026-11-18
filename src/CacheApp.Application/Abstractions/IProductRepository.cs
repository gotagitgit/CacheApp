using CacheApp.Domain.Products;

namespace CacheApp.Application.Abstractions;

public interface IProductRepository
{
    Task<ProductPageData> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Product?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(Product product, CancellationToken cancellationToken);

    Task UpdateAsync(Product product, CancellationToken cancellationToken);

    void Remove(Product product);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
