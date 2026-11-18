using CacheApp.Application.Abstractions;

namespace CacheApp.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health/live", () => Results.Ok(new { status = "live" }))
            .WithTags("Health")
            .WithName("HealthLive");

        endpoints.MapGet("/health/ready", CheckReadinessAsync)
            .WithTags("Health")
            .WithName("HealthReady");

        return endpoints;
    }

    private static async Task<IResult> CheckReadinessAsync(
        IReadinessProbe readinessProbe,
        CancellationToken cancellationToken)
    {
        if (!await readinessProbe.IsReadyAsync(cancellationToken))
        {
            return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Dependencies are not ready.");
        }

        return Results.Ok(new { status = "ready" });
    }
}
