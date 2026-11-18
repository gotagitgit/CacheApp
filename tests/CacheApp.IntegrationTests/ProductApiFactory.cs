using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CacheApp.IntegrationTests;

public sealed class ProductApiFactory : WebApplicationFactory<Program>
{
    private readonly string _cacheNamespace = $"product-cache-test-{Guid.NewGuid():N}:";
    private readonly string _invalidationChannel = $"product-cache-test-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var values = new Dictionary<string, string?>
            {
                ["ConnectionStrings:ProductDatabase"] = Environment.GetEnvironmentVariable("ConnectionStrings__ProductDatabase"),
                ["ConnectionStrings:Redis"] = Environment.GetEnvironmentVariable("ConnectionStrings__Redis"),
                ["Cache:InvalidationChannel"] = _invalidationChannel,
                ["Cache:RedisInstanceName"] = _cacheNamespace
            };

            configuration.AddInMemoryCollection(values);
        });
    }
}
