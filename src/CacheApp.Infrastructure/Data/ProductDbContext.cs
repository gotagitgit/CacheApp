using CacheApp.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace CacheApp.Infrastructure.Data;

public sealed class ProductDbContext(DbContextOptions<ProductDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var product = modelBuilder.Entity<Product>();
        product.ToTable("Products");
        product.HasKey(item => item.Id);
        product.Property(item => item.Id).ValueGeneratedNever();
        product.Property(item => item.Name).HasMaxLength(120).IsRequired();
        product.Property(item => item.Description).HasMaxLength(500);
        product.Property(item => item.Price).HasPrecision(18, 2);
        product.Property(item => item.StockQuantity).IsRequired();
        product.HasData(ProductSeedData.Products);
    }
}
