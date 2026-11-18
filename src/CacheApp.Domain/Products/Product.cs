namespace CacheApp.Domain.Products;

public sealed class Product
{
    public Product(Guid id, string name, string? description, decimal price, int stockQuantity)
    {
        ArgumentNullException.ThrowIfNull(name);

        Id = id;
        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
    }

    public Guid Id { get; }

    public string Name { get; }

    public string? Description { get; }

    public decimal Price { get; }

    public int StockQuantity { get; }
}
