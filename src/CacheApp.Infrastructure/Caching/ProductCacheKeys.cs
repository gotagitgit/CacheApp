namespace CacheApp.Infrastructure.Caching;

public static class ProductCacheKeys
{
    public const string ProductTag = "products";
    public const string ProductListTag = "products:list";

    public static string ForItem(Guid id) => $"products:item:{id:D}";

    public static string ForItemTag(Guid id) => $"products:item:{id:D}";

    public static string ForList(int page, int pageSize) => $"products:list:{page}:{pageSize}";
}
