using CacheApp.Infrastructure.Caching;

namespace CacheApp.UnitTests;

public sealed class ProductCacheKeysTests
{
    [Fact]
    public void ForItem_UsesCanonicalGuidFormat()
    {
        var productId = Guid.Parse("10000000-0000-0000-0000-000000000001");

        var key = ProductCacheKeys.ForItem(productId);

        key.Should().Be("products:item:10000000-0000-0000-0000-000000000001");
    }

    [Fact]
    public void ForList_IncludesPageAndPageSize()
    {
        var key = ProductCacheKeys.ForList(2, 25);

        key.Should().Be("products:list:2:25");
    }
}
