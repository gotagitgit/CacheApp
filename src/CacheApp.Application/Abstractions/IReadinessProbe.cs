namespace CacheApp.Application.Abstractions;

public interface IReadinessProbe
{
    Task<bool> IsReadyAsync(CancellationToken cancellationToken);
}
