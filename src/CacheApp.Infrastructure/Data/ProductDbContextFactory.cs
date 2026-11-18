using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CacheApp.Infrastructure.Data;

public sealed class ProductDbContextFactory : IDesignTimeDbContextFactory<ProductDbContext>
{
    private const string DesignTimeConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=ProductCaching;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

    public ProductDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<ProductDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();
        var connectionString = configuration.GetConnectionString("ProductDatabase") ?? DesignTimeConnectionString;

        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ProductDbContext(options);
    }
}
