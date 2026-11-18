using CacheApp.Application.Abstractions;
using CacheApp.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CacheApp.Infrastructure.Data;

public sealed class EfProductRepository(ProductDbContext database, ILogger<EfProductRepository> logger) : IProductRepository
{
    public async Task<ProductPageData> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        logger.LogInformation("Loading product page {Page} with page size {PageSize} from SQL Server after a cache miss.", page, pageSize);

        var query = database.Products.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);
        var skipCount = (long)(page - 1) * pageSize;
        var products = skipCount > int.MaxValue
            ? []
            : await query
                .OrderBy(product => product.Id)
                .Skip((int)skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

        return new ProductPageData(products, totalCount);
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Loading product {ProductId} from SQL Server after a cache miss.", id);
        return await database.Products.AsNoTracking().FirstOrDefaultAsync(product => product.Id == id, cancellationToken);
    }

    public Task<Product?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken) =>
        database.Products.FirstOrDefaultAsync(product => product.Id == id, cancellationToken);

    public async Task AddAsync(Product product, CancellationToken cancellationToken) =>
        await database.Products.AddAsync(product, cancellationToken);

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken)
    {
        database.Products.Update(product);
        await database.SaveChangesAsync(cancellationToken);
    }

    public void Remove(Product product) => database.Products.Remove(product);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => database.SaveChangesAsync(cancellationToken);
}
