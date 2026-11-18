using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace CacheApp.UnitTests;

public sealed class HybridCacheStampedeTests
{
    [Fact]
    public async Task GetOrCreateAsync_CoalescesConcurrentFactoryCallsInOneProcess()
    {
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddHybridCache();

        using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();
        var factoryEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFactory = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var factoryCalls = 0;

        async ValueTask<int> Factory(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref factoryCalls);
            factoryEntered.TrySetResult();
            await releaseFactory.Task.WaitAsync(cancellationToken);
            return 42;
        }

        var requests = Enumerable.Range(0, 20)
            .Select(_ => cache.GetOrCreateAsync("stampede:test", Factory).AsTask())
            .ToArray();

        await factoryEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        releaseFactory.TrySetResult();
        var values = await Task.WhenAll(requests);

        Assert.All(values, value => Assert.Equal(42, value));
        Assert.Equal(1, factoryCalls);
    }
}
