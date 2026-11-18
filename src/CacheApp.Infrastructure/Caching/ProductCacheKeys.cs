namespace CacheApp.Infrastructure.Caching;

public static class ProductCacheKeys
{
    public const string ProductTag = "products";
    public const string ProductListTag = $"{ProductTag}:list";
    private const string ProductItemTag = $"{ProductTag}:item";

    public static string ForItem(Guid id) => $"{ProductItemTag}:{id:D}";

    public static string ForItemTag(Guid id) => $"{ProductItemTag}:{id:D}";

    public static string ForList(int page, int pageSize) => $"{ProductListTag}:{page}:{pageSize}";
}
