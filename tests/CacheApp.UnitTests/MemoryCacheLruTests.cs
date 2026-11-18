using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;

namespace CacheApp.UnitTests;

public sealed class MemoryCacheLruTests
{
    [Fact]
    public void Compact_RemovesLeastRecentlyUsedEntryWithinPriorityBucket()
    {
        var clock = new TestClock(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        using var cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 3,
            CompactionPercentage = 0.34,
            Clock = clock
        });

        cache.Set("cold", "cold", new MemoryCacheEntryOptions().SetSize(1));
        clock.Advance(TimeSpan.FromSeconds(1));
        cache.Set("least-recent", "least-recent", new MemoryCacheEntryOptions().SetSize(1));
        clock.Advance(TimeSpan.FromSeconds(1));
        cache.Get<string>("cold").Should().Be("cold");
        clock.Advance(TimeSpan.FromSeconds(1));
        cache.Set("newest", "newest", new MemoryCacheEntryOptions().SetSize(1));

        cache.Compact(1d / 3d);

        cache.Get<string>("least-recent").Should().BeNull();
        cache.Get<string>("cold").Should().Be("cold");
        cache.Get<string>("newest").Should().Be("newest");
    }

    private sealed class TestClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; private set; } = utcNow;

        public void Advance(TimeSpan elapsed) => UtcNow += elapsed;
    }
}
