using CacheApp.Application.Products;
using Microsoft.AspNetCore.Mvc;

namespace CacheApp.Api.Endpoints;

public static class ProductEndpoints
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 10;
    private const int MaximumPageSize = 50;

    public static RouteGroupBuilder MapProductEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/products").WithTags("Products");

        group.MapGet("", GetPageAsync)
            .WithName("GetProducts")
            .WithSummary("List products using page and pageSize pagination.");
        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetProductById")
            .WithSummary("Retrieve one product by its GUID.");
        group.MapPost("", CreateAsync)
            .WithName("CreateProduct")
            .WithSummary("Create a product.");
        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateProduct")
            .WithSummary("Update a product.");
        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteProduct")
            .WithSummary("Delete a product.");

        return group;
    }

    private static async Task<IResult> GetPageAsync(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        ProductService service,
        CancellationToken cancellationToken)
    {
        var requestedPage = page ?? DefaultPage;
        var requestedPageSize = pageSize ?? DefaultPageSize;
        var errors = new Dictionary<string, string[]>();

        if (requestedPage < 1)
        {
            errors[nameof(page)] = ["Page must be greater than or equal to 1."];
        }

        if (requestedPageSize is < 1 or > MaximumPageSize)
        {
            errors[nameof(pageSize)] = [$"Page size must be between 1 and {MaximumPageSize}."];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var result = await service.GetPageAsync(requestedPage, requestedPageSize, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetByIdAsync(Guid id, ProductService service, CancellationToken cancellationToken)
    {
        var product = await service.GetByIdAsync(id, cancellationToken);
        return product is null ? Results.NotFound() : Results.Ok(product);
    }

    private static async Task<IResult> CreateAsync(
        ProductRequest request,
        ProductService service,
        CancellationToken cancellationToken)
    {
        var errors = ProductRequestValidator.Validate(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var product = await service.CreateAsync(request, cancellationToken);
        return Results.CreatedAtRoute("GetProductById", new { id = product.Id }, product);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        ProductRequest request,
        ProductService service,
        CancellationToken cancellationToken)
    {
        var errors = ProductRequestValidator.Validate(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var product = await service.UpdateAsync(id, request, cancellationToken);
        return product is null ? Results.NotFound() : Results.NoContent();
    }

    private static async Task<IResult> DeleteAsync(Guid id, ProductService service, CancellationToken cancellationToken)
    {
        var deleted = await service.DeleteAsync(id, cancellationToken);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}
