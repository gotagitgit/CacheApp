namespace CacheApp.Application.Products;

public static class ProductRequestValidator
{
    private const int MaximumNameLength = 120;
    private const int MaximumDescriptionLength = 500;

    public static Dictionary<string, string[]> Validate(ProductRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors[nameof(request.Name)] = ["Name is required."];
        }
        else if (request.Name.Length > MaximumNameLength)
        {
            errors[nameof(request.Name)] = [$"Name must be at most {MaximumNameLength} characters."];
        }

        if (request.Description?.Length > MaximumDescriptionLength)
        {
            errors[nameof(request.Description)] = [$"Description must be at most {MaximumDescriptionLength} characters."];
        }

        if (request.Price < 0)
        {
            errors[nameof(request.Price)] = ["Price cannot be negative."];
        }

        if (request.StockQuantity < 0)
        {
            errors[nameof(request.StockQuantity)] = ["Stock quantity cannot be negative."];
        }

        return errors;
    }
}
