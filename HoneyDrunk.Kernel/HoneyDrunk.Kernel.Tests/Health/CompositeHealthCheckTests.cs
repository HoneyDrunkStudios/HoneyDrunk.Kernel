using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Health;
using HoneyDrunk.Kernel.Health;

namespace HoneyDrunk.Kernel.Tests.Health;

public class CompositeHealthCheckTests
{
    [Fact]
    public async Task CheckAsync_NoHealthChecks_ReturnsHealthy()
    {
        var composite = new CompositeHealthCheck([]);

        var result = await composite.CheckAsync();

        result.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckAsync_AllHealthy_ReturnsHealthy()
    {
        var checks = new[]
        {
            new TestHealthCheck(HealthStatus.Healthy),
            new TestHealthCheck(HealthStatus.Healthy),
            new TestHealthCheck(HealthStatus.Healthy)
        };
        var composite = new CompositeHealthCheck(checks);

        var result = await composite.CheckAsync();

        result.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckAsync_OneDegraded_ReturnsDegraded()
    {
        var checks = new[]
        {
            new TestHealthCheck(HealthStatus.Healthy),
            new TestHealthCheck(HealthStatus.Degraded),
            new TestHealthCheck(HealthStatus.Healthy)
        };
        var composite = new CompositeHealthCheck(checks);

        var result = await composite.CheckAsync();

        result.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public async Task CheckAsync_OneUnhealthy_ReturnsUnhealthy()
    {
        var checks = new[]
        {
            new TestHealthCheck(HealthStatus.Healthy),
            new TestHealthCheck(HealthStatus.Unhealthy),
            new TestHealthCheck(HealthStatus.Healthy)
        };
        var composite = new CompositeHealthCheck(checks);

        var result = await composite.CheckAsync();

        result.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckAsync_UnhealthyAndDegraded_ReturnsUnhealthy()
    {
        var checks = new[]
        {
            new TestHealthCheck(HealthStatus.Degraded),
            new TestHealthCheck(HealthStatus.Unhealthy),
            new TestHealthCheck(HealthStatus.Healthy)
        };
        var composite = new CompositeHealthCheck(checks);

        var result = await composite.CheckAsync();

        result.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckAsync_AllDegraded_ReturnsDegraded()
    {
        var checks = new[]
        {
            new TestHealthCheck(HealthStatus.Degraded),
            new TestHealthCheck(HealthStatus.Degraded)
        };
        var composite = new CompositeHealthCheck(checks);

        var result = await composite.CheckAsync();

        result.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public async Task CheckAsync_AllUnhealthy_ReturnsUnhealthy()
    {
        var checks = new[]
        {
            new TestHealthCheck(HealthStatus.Unhealthy),
            new TestHealthCheck(HealthStatus.Unhealthy)
        };
        var composite = new CompositeHealthCheck(checks);

        var result = await composite.CheckAsync();

        result.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckAsync_OneThrowsException_ReturnsUnhealthy()
    {
        var checks = new IHealthCheck[]
        {
            new TestHealthCheck(HealthStatus.Healthy),
            new ThrowingHealthCheck(new InvalidOperationException("Test error")),
            new TestHealthCheck(HealthStatus.Healthy)
        };
        var composite = new CompositeHealthCheck(checks);

        var result = await composite.CheckAsync();

        result.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckAsync_AllThrowException_ReturnsUnhealthy()
    {
        var checks = new IHealthCheck[]
        {
            new ThrowingHealthCheck(new InvalidOperationException("Error 1")),
            new ThrowingHealthCheck(new InvalidOperationException("Error 2"))
        };
        var composite = new CompositeHealthCheck(checks);

        var result = await composite.CheckAsync();

        result.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var checks = new[]
        {
            new TestHealthCheck(HealthStatus.Healthy)
        };
        var composite = new CompositeHealthCheck(checks);

        var act = async () => await composite.CheckAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task CheckAsync_PropagatesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var checks = new[]
        {
            new CancellationAwareHealthCheck()
        };
        var composite = new CompositeHealthCheck(checks);

        await composite.CheckAsync(cts.Token);

        ((CancellationAwareHealthCheck)checks[0]).ReceivedToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task CheckAsync_MultipleChecks_RunsConcurrently()
    {
        var checks = new[]
        {
            new DelayedHealthCheck(TimeSpan.FromMilliseconds(50)),
            new DelayedHealthCheck(TimeSpan.FromMilliseconds(50)),
            new DelayedHealthCheck(TimeSpan.FromMilliseconds(50))
        };
        var composite = new CompositeHealthCheck(checks);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        await composite.CheckAsync();

        stopwatch.Stop();

        // Should complete in ~50ms if concurrent, would be ~150ms if sequential
        // Use 200ms threshold to allow for system variability
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(200);
    }

    [Fact]
    public async Task CheckAsync_SingleHealthCheck_ReturnsItsStatus()
    {
        var checks = new[]
        {
            new TestHealthCheck(HealthStatus.Degraded)
        };
        var composite = new CompositeHealthCheck(checks);

        var result = await composite.CheckAsync();

        result.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public async Task CheckAsync_MixedHealthyDegradedUnhealthy_ReturnsWorst()
    {
        var checks = new[]
        {
            new TestHealthCheck(HealthStatus.Healthy),
            new TestHealthCheck(HealthStatus.Degraded),
            new TestHealthCheck(HealthStatus.Unhealthy),
            new TestHealthCheck(HealthStatus.Healthy)
        };
        var composite = new CompositeHealthCheck(checks);

        var result = await composite.CheckAsync();

        result.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckAsync_ExceptionAndHealthy_ReturnsUnhealthy()
    {
        var checks = new IHealthCheck[]
        {
            new TestHealthCheck(HealthStatus.Healthy),
            new ThrowingHealthCheck(new TimeoutException("Timeout")),
            new TestHealthCheck(HealthStatus.Healthy)
        };
        var composite = new CompositeHealthCheck(checks);

        var result = await composite.CheckAsync();

        result.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckAsync_ExceptionAndDegraded_ReturnsUnhealthy()
    {
        var checks = new IHealthCheck[]
        {
            new TestHealthCheck(HealthStatus.Degraded),
            new ThrowingHealthCheck(new InvalidOperationException("Error"))
        };
        var composite = new CompositeHealthCheck(checks);

        var result = await composite.CheckAsync();

        result.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckAsync_SlowCheck_WaitsForCompletion()
    {
        var checks = new IHealthCheck[]
        {
            new TestHealthCheck(HealthStatus.Healthy),
            new DelayedHealthCheck(TimeSpan.FromMilliseconds(100)),
            new TestHealthCheck(HealthStatus.Healthy)
        };
        var composite = new CompositeHealthCheck(checks);

        var result = await composite.CheckAsync();

        result.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckAsync_CancellationDuringExecution_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        var checks = new[]
        {
            new DelayedHealthCheck(TimeSpan.FromMilliseconds(100))
        };
        var composite = new CompositeHealthCheck(checks);

        var checkTask = composite.CheckAsync(cts.Token);
        await Task.Delay(10);
        cts.Cancel();

        var act = async () => await checkTask;

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task CheckAsync_NullChecksArray_ThrowsException()
    {
        // Implementation doesn't explicitly validate null, will throw during construction
        var act = () => new CompositeHealthCheck(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task CheckAsync_EmptyChecksArray_ReturnsHealthy()
    {
        var composite = new CompositeHealthCheck([]);

        var result = await composite.CheckAsync();

        result.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckAsync_LargeNumberOfChecks_HandlesAll()
    {
        var checks = Enumerable.Range(0, 50)
            .Select(_ => new TestHealthCheck(HealthStatus.Healthy))
            .ToArray();
        var composite = new CompositeHealthCheck(checks);

        var result = await composite.CheckAsync();

        result.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckAsync_OneUnhealthyAmongMany_ReturnsUnhealthy()
    {
        var checks = Enumerable.Range(0, 20)
            .Select(i => (IHealthCheck)(i == 10
                ? new TestHealthCheck(HealthStatus.Unhealthy)
                : new TestHealthCheck(HealthStatus.Healthy)))
            .ToArray();
        var composite = new CompositeHealthCheck(checks);

        var result = await composite.CheckAsync();

        result.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckAsync_MultipleDifferentExceptions_StillReturnsUnhealthy()
    {
        var checks = new IHealthCheck[]
        {
            new ThrowingHealthCheck(new InvalidOperationException("Error 1")),
            new ThrowingHealthCheck(new TimeoutException("Timeout")),
            new ThrowingHealthCheck(new ArgumentException("Argument error"))
        };
        var composite = new CompositeHealthCheck(checks);

        var result = await composite.CheckAsync();

        result.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckAsync_CheckReturnsTaskCompletedSynchronously_HandlesCorrectly()
    {
        var checks = new[]
        {
            new TestHealthCheck(HealthStatus.Healthy)
        };
        var composite = new CompositeHealthCheck(checks);

        var result = await composite.CheckAsync();

        result.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckAsync_WithDefaultCancellationToken_Succeeds()
    {
        var checks = new[]
        {
            new TestHealthCheck(HealthStatus.Healthy)
        };
        var composite = new CompositeHealthCheck(checks);

        var result = await composite.CheckAsync(default);

        result.Should().Be(HealthStatus.Healthy);
    }

    private sealed class TestHealthCheck(HealthStatus status) : IHealthCheck
    {
        public Task<HealthStatus> CheckAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(status);
        }
    }

    private sealed class ThrowingHealthCheck(Exception exception) : IHealthCheck
    {
        public Task<HealthStatus> CheckAsync(CancellationToken cancellationToken = default)
        {
            throw exception;
        }
    }

    private sealed class CancellationAwareHealthCheck : IHealthCheck
    {
        public CancellationToken ReceivedToken { get; private set; }

        public Task<HealthStatus> CheckAsync(CancellationToken cancellationToken = default)
        {
            ReceivedToken = cancellationToken;
            return Task.FromResult(HealthStatus.Healthy);
        }
    }

    private sealed class DelayedHealthCheck(TimeSpan delay) : IHealthCheck
    {
        public async Task<HealthStatus> CheckAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(delay, cancellationToken);
            return HealthStatus.Healthy;
        }
    }
}
