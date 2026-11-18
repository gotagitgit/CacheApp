using CacheApp.Infrastructure.Caching;

namespace CacheApp.UnitTests;

public sealed class ProductCacheKeysTests
{
    [Fact]
    public void ForItem_UsesCanonicalGuidFormat()
    {
        var productId = Guid.Parse("10000000-0000-0000-0000-000000000001");

        var key = ProductCacheKeys.ForItem(productId);

        Assert.Equal("products:item:10000000-0000-0000-0000-000000000001", key);
    }

    [Fact]
    public void ForList_IncludesPageAndPageSize()
    {
        var key = ProductCacheKeys.ForList(2, 25);

        Assert.Equal("products:list:2:25", key);
    }
}
