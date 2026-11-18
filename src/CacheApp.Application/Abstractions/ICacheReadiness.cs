namespace CacheApp.Application.Abstractions;

public interface ICacheReadiness
{
    bool IsReady { get; }

    Task WaitUntilReadyAsync(CancellationToken cancellationToken);
}
