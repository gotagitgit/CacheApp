using CacheApp.Infrastructure.Configuration;

namespace CacheApp.UnitTests;

public sealed class CacheReadinessTests
{
    [Fact]
    public async Task WaitUntilReadyAsync_ReleasesWaitingReadsAfterResetCompletes()
    {
        var readiness = new CacheReadiness();
        var waitingRead = readiness.WaitUntilReadyAsync(CancellationToken.None);

        Assert.False(waitingRead.IsCompleted);

        readiness.MarkReady();
        await waitingRead;

        readiness.MarkUnavailable();
        Assert.False(readiness.IsReady);

        var readAfterReconnect = readiness.WaitUntilReadyAsync(CancellationToken.None);
        Assert.False(readAfterReconnect.IsCompleted);

        readiness.MarkReady();
        await readAfterReconnect;
    }
}
