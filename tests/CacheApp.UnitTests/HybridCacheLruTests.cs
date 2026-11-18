using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;

namespace CacheApp.UnitTests;

public sealed class HybridCacheLruTests
{
    [Fact]
    public async Task MemoryCompaction_EvictsLeastRecentlyUsedHybridCacheEntry()
    {
        var clock = new TestClock(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var services = new ServiceCollection();
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 2;
            options.CompactionPercentage = 0.5;
            options.Clock = clock;
        });
        services.AddHybridCache();

        using var provider = services.BuildServiceProvider();
        var hybridCache = provider.GetRequiredService<HybridCache>();
        var memoryCache = Assert.IsType<MemoryCache>(provider.GetRequiredService<IMemoryCache>());
        var leastRecentFactoryCalls = 0;

        await hybridCache.GetOrCreateAsync(
            "lru:cold",
            "cold",
            static (state, _) => ValueTask.FromResult(state),
            EntryOptions,
            cancellationToken: TestContext.Current.CancellationToken);
        clock.Advance(TimeSpan.FromSeconds(1));
        await hybridCache.GetOrCreateAsync(
            "lru:least-recent",
            "least-recent",
            static (state, _) => ValueTask.FromResult(state),
            EntryOptions,
            cancellationToken: TestContext.Current.CancellationToken);
        clock.Advance(TimeSpan.FromSeconds(1));
        await hybridCache.GetOrCreateAsync(
            "lru:cold",
            "unused",
            static (state, _) => ValueTask.FromResult(state),
            EntryOptions,
            cancellationToken: TestContext.Current.CancellationToken);

        memoryCache.Compact(0.5);

        var value = await hybridCache.GetOrCreateAsync(
            "lru:least-recent",
            "least-recent",
            (state, cancellationToken) =>
            {
                Interlocked.Increment(ref leastRecentFactoryCalls);
                return ValueTask.FromResult(state);
            },
            EntryOptions,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("least-recent", value);
        Assert.Equal(1, leastRecentFactoryCalls);
    }

    private static HybridCacheEntryOptions EntryOptions => new()
    {
        Expiration = TimeSpan.FromMinutes(1),
        LocalCacheExpiration = TimeSpan.FromMinutes(1),
        LocalSize = 1
    };

    private sealed class TestClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; private set; } = utcNow;

        public void Advance(TimeSpan elapsed) => UtcNow += elapsed;
    }
}
