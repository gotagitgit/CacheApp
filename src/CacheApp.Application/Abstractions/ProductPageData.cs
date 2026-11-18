using CacheApp.Domain.Products;

namespace CacheApp.Application.Abstractions;

public sealed record ProductPageData(IReadOnlyList<Product> Products, int TotalCount);
