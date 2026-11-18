namespace CacheApp.Application.Products;

public sealed record ProductRequest(
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity);

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity);

public sealed record ProductPageResponse(
    IReadOnlyList<ProductResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
