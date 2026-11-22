using System.Net;
using CacheApp.Application.Products;

namespace CacheApp.IntegrationTests;

[Collection(IntegrationTestCollection.CollectionName)]
public sealed class ProductCacheCrossProcessTests(IntegrationTestFixture fixture)
{
    private const int ListPageSize = 20;

    [RequiresExternalDependencies]
    public async Task Write_in_one_host_invalidates_detail_and_list_cache_in_another_host()
    {
        using var firstFactory = new ProductApiFactory(fixture.CreateApiSettings());
        using var secondFactory = new ProductApiFactory(fixture.CreateApiSettings());
        using var firstClient = firstFactory.CreateClient();
        using var secondClient = secondFactory.CreateClient();

        var createRequest = new ProductRequest(
            "Original cross-process product",
            "Created by the integration test",
            49.95m,
            7);
        var createResponse = await firstClient.PostAsJsonAsync("/api/products", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var original = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();
        original.Should().NotBeNull();
        var productId = original!.Id;
        var cachedListPage = 1;

        try
        {
            var cachedProduct = await secondClient.GetFromJsonAsync<ProductResponse>($"/api/products/{productId}");
            cachedProduct.Should().BeEquivalentTo(original);

            var firstPage = await secondClient.GetFromJsonAsync<ProductPageResponse>(
                $"/api/products?page=1&pageSize={ListPageSize}");
            firstPage.Should().NotBeNull();

            if (firstPage!.Items.All(item => item.Id != productId))
            {
                cachedListPage = 2;
            }

            var cachedPage = cachedListPage == 1
                ? firstPage
                : await secondClient.GetFromJsonAsync<ProductPageResponse>(
                    $"/api/products?page={cachedListPage}&pageSize={ListPageSize}");
            cachedPage.Should().NotBeNull();
            cachedPage!.Items.Should().Contain(item => item.Id == productId);

            var updatedRequest = createRequest with { Name = "Updated across processes" };
            var updateResponse = await firstClient.PutAsJsonAsync($"/api/products/{productId}", updatedRequest);
            updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            await WaitForUpdatedProductAsync(secondClient, productId, updatedRequest.Name);
            await WaitForUpdatedListItemAsync(secondClient, cachedListPage, productId, updatedRequest.Name);
        }
        finally
        {
            await firstClient.DeleteAsync($"/api/products/{productId}");
        }
    }

    private static async Task WaitForUpdatedProductAsync(HttpClient client, Guid productId, string expectedName)
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        try
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                var product = await client.GetFromJsonAsync<ProductResponse>(
                    $"/api/products/{productId}",
                    cancellationTokenSource.Token);
                if (product?.Name == expectedName)
                {
                    return;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(20), cancellationTokenSource.Token);
            }
        }
        catch (OperationCanceledException exception) when (cancellationTokenSource.IsCancellationRequested)
        {
            throw new TimeoutException(
                "The API host reading the product detail did not observe the invalidation.",
                exception);
        }
    }

    private static async Task WaitForUpdatedListItemAsync(
        HttpClient client,
        int page,
        Guid productId,
        string expectedName)
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        try
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                var productPage = await client.GetFromJsonAsync<ProductPageResponse>(
                    $"/api/products?page={page}&pageSize={ListPageSize}",
                    cancellationTokenSource.Token);
                if (productPage?.Items.Any(item => item.Id == productId && item.Name == expectedName) == true)
                {
                    return;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(20), cancellationTokenSource.Token);
            }
        }
        catch (OperationCanceledException exception) when (cancellationTokenSource.IsCancellationRequested)
        {
            throw new TimeoutException(
                "The API host reading the product list did not observe the invalidation.",
                exception);
        }
    }
}
