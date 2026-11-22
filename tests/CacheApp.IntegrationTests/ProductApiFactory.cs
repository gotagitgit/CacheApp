using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CacheApp.IntegrationTests;

public sealed class ProductApiFactory(ProductApiTestSettings settings) : WebApplicationFactory<Program>
{
    private const string TestLocalTtlSeconds = "120";
    private const string TestDistributedTtlSeconds = "180";
    private const string TestLocalSizeLimit = "100";

    protected override void ConfigureWebApplicationBuilder(IHostApplicationBuilder builder)
    {
        builder.Environment.EnvironmentName = Environments.Development;
        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:ProductDatabase"] = settings.ProductDatabaseConnectionString,
                ["ConnectionStrings:Redis"] = settings.RedisConnectionString,
                ["Cache:InvalidationChannel"] = settings.InvalidationChannel,
                ["Cache:RedisInstanceName"] = settings.RedisInstanceName,
                ["Cache:LocalTtlSeconds"] = TestLocalTtlSeconds,
                ["Cache:DistributedTtlSeconds"] = TestDistributedTtlSeconds,
                ["Cache:LocalSizeLimit"] = TestLocalSizeLimit
            });

        base.ConfigureWebApplicationBuilder(builder);
    }
}
