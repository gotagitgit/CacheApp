using CacheApp.Infrastructure.Configuration;

namespace CacheApp.UnitTests;

public sealed class CacheReadinessTests
{
    [Fact]
    public async Task WaitUntilReadyAsync_ReleasesWaitingReadsAfterResetCompletes()
    {
        var readiness = new CacheReadiness();
        var waitingRead = readiness.WaitUntilReadyAsync(CancellationToken.None);

        waitingRead.IsCompleted.Should().BeFalse();

        readiness.MarkReady();
        await waitingRead;

        readiness.MarkUnavailable();
        readiness.IsReady.Should().BeFalse();

        var readAfterReconnect = readiness.WaitUntilReadyAsync(CancellationToken.None);
        readAfterReconnect.IsCompleted.Should().BeFalse();

        readiness.MarkReady();
        await readAfterReconnect;
    }
}
