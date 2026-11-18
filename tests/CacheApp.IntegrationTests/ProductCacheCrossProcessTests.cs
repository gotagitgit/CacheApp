using System.Net;
using CacheApp.Application.Products;

namespace CacheApp.IntegrationTests;

public sealed class ProductCacheCrossProcessTests
{
    private static readonly Guid SeedProductId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    [RequiresExternalDependencies]
    public async Task Write_in_one_host_invalidates_detail_and_list_cache_in_another_host()
    {
        using var firstFactory = new ProductApiFactory();
        using var secondFactory = new ProductApiFactory();
        using var firstClient = firstFactory.CreateClient();
        using var secondClient = secondFactory.CreateClient();

        var original = await secondClient.GetFromJsonAsync<ProductResponse>($"/api/products/{SeedProductId}");
        Assert.NotNull(original);

        var originalPage = await secondClient.GetFromJsonAsync<ProductPageResponse>("/api/products?page=1&pageSize=20");
        Assert.NotNull(originalPage);
        Assert.Contains(originalPage.Items, item => item.Id == SeedProductId);

        var updated = new ProductRequest("Updated across processes", original.Description, original.Price, original.StockQuantity);

        try
        {
            var updateResponse = await firstClient.PutAsJsonAsync($"/api/products/{SeedProductId}", updated);
            Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

            await WaitForUpdatedProductAsync(secondClient, "Updated across processes");
            await WaitForUpdatedListItemAsync(secondClient, "Updated across processes");
        }
        finally
        {
            var restoreRequest = new ProductRequest(original.Name, original.Description, original.Price, original.StockQuantity);
            await firstClient.PutAsJsonAsync($"/api/products/{SeedProductId}", restoreRequest);
        }
    }

    private static async Task WaitForUpdatedProductAsync(HttpClient client, string expectedName)
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        while (!cancellationTokenSource.IsCancellationRequested)
        {
            var product = await client.GetFromJsonAsync<ProductResponse>(
                $"/api/products/{SeedProductId}",
                cancellationTokenSource.Token);
            if (product?.Name == expectedName)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(20), cancellationTokenSource.Token);
        }

        throw new TimeoutException("The second API host did not observe the product detail invalidation.");
    }

    private static async Task WaitForUpdatedListItemAsync(HttpClient client, string expectedName)
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        while (!cancellationTokenSource.IsCancellationRequested)
        {
            var page = await client.GetFromJsonAsync<ProductPageResponse>(
                "/api/products?page=1&pageSize=20",
                cancellationTokenSource.Token);
            if (page?.Items.Any(item => item.Id == SeedProductId && item.Name == expectedName) == true)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(20), cancellationTokenSource.Token);
        }

        throw new TimeoutException("The second API host did not observe the product list invalidation.");
    }
}
