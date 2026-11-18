using CacheApp.Application.Products;

namespace CacheApp.UnitTests;

public sealed class ProductRequestValidatorTests
{
    [Fact]
    public void Validate_ReturnsNoErrorsForValidProduct()
    {
        var request = new ProductRequest("Desk lamp", "Adjustable LED lamp", 32.50m, 12);

        var errors = ProductRequestValidator.Validate(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ReturnsErrorsForInvalidProductFields()
    {
        var request = new ProductRequest(" ", new string('x', 501), -1m, -2);

        var errors = ProductRequestValidator.Validate(request);

        Assert.Contains(nameof(ProductRequest.Name), errors.Keys);
        Assert.Contains(nameof(ProductRequest.Description), errors.Keys);
        Assert.Contains(nameof(ProductRequest.Price), errors.Keys);
        Assert.Contains(nameof(ProductRequest.StockQuantity), errors.Keys);
    }
}
