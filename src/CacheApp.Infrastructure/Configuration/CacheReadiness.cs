using CacheApp.Application.Abstractions;

namespace CacheApp.Infrastructure.Configuration;

public sealed class CacheReadiness : ICacheReadiness
{
    private readonly object _sync = new();
    private TaskCompletionSource _readySignal = NewSignal();
    private bool _isReady;

    public bool IsReady
    {
        get
        {
            lock (_sync)
            {
                return _isReady;
            }
        }
    }

    public Task WaitUntilReadyAsync(CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            return _isReady ? Task.CompletedTask : _readySignal.Task.WaitAsync(cancellationToken);
        }
    }

    public void MarkUnavailable()
    {
        lock (_sync)
        {
            if (_isReady)
            {
                _isReady = false;
                _readySignal = NewSignal();
            }
        }
    }

    public void MarkReady()
    {
        lock (_sync)
        {
            _isReady = true;
            _readySignal.TrySetResult();
        }
    }

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
