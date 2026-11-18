using CacheApp.Application.Products;

namespace CacheApp.UnitTests;

public sealed class ProductRequestValidatorTests
{
    [Fact]
    public void Validate_ReturnsNoErrorsForValidProduct()
    {
        var request = new ProductRequest("Desk lamp", "Adjustable LED lamp", 32.50m, 12);

        var errors = ProductRequestValidator.Validate(request);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ReturnsErrorsForInvalidProductFields()
    {
        var request = new ProductRequest(" ", new string('x', 501), -1m, -2);

        var errors = ProductRequestValidator.Validate(request);

        errors.Keys.Should().Contain(nameof(ProductRequest.Name));
        errors.Keys.Should().Contain(nameof(ProductRequest.Description));
        errors.Keys.Should().Contain(nameof(ProductRequest.Price));
        errors.Keys.Should().Contain(nameof(ProductRequest.StockQuantity));
    }
}
