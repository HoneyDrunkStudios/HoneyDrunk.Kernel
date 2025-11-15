using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Health;
using HoneyDrunk.Kernel.Health;

namespace HoneyDrunk.Kernel.Tests.Health;

/// <summary>
/// Tests for <see cref="CompositeHealthCheck"/>.
/// </summary>
public class CompositeHealthCheckTests
{
    /// <summary>
    /// Returns healthy when no checks are provided.
    /// </summary>
    /// <returns>A completed task.</returns>
    [Fact]
    public async Task CheckAsync_WithNoChecks_ReturnsHealthy()
    {
        var composite = new CompositeHealthCheck([]);
        var status = await composite.CheckAsync();
        status.Should().Be(HealthStatus.Healthy);
    }

    /// <summary>
    /// Worst status wins: Unhealthy overrides other results.
    /// </summary>
    /// <returns>A completed task.</returns>
    [Fact]
    public async Task CheckAsync_ReturnsWorstStatus_UnhealthyBeatsDegradedAndHealthy()
    {
        var checks = new IHealthCheck[]
        {
            new StaticHealthCheck(HealthStatus.Healthy),
            new StaticHealthCheck(HealthStatus.Degraded),
            new StaticHealthCheck(HealthStatus.Unhealthy),
        };
        var composite = new CompositeHealthCheck(checks);
        var status = await composite.CheckAsync();
        status.Should().Be(HealthStatus.Unhealthy);
    }

    /// <summary>
    /// Returns degraded when any check is degraded and none unhealthy.
    /// </summary>
    /// <returns>A completed task.</returns>
    [Fact]
    public async Task CheckAsync_ReturnsDegraded_WhenAnyIsDegraded_AndNoneUnhealthy()
    {
        var checks = new IHealthCheck[]
        {
            new StaticHealthCheck(HealthStatus.Healthy),
            new StaticHealthCheck(HealthStatus.Degraded),
            new StaticHealthCheck(HealthStatus.Healthy),
        };
        var composite = new CompositeHealthCheck(checks);
        var status = await composite.CheckAsync();
        status.Should().Be(HealthStatus.Degraded);
    }

    /// <summary>
    /// Returns healthy when all checks are healthy.
    /// </summary>
    /// <returns>A completed task.</returns>
    [Fact]
    public async Task CheckAsync_AllHealthy_ReturnsHealthy()
    {
        var checks = new IHealthCheck[]
        {
            new StaticHealthCheck(HealthStatus.Healthy),
            new StaticHealthCheck(HealthStatus.Healthy),
        };
        var composite = new CompositeHealthCheck(checks);
        var status = await composite.CheckAsync();
        status.Should().Be(HealthStatus.Healthy);
    }

    /// <summary>
    /// Exceptions from individual checks are treated as Unhealthy.
    /// </summary>
    /// <returns>A completed task.</returns>
    [Fact]
    public async Task CheckAsync_TreatsExceptionsAsUnhealthy()
    {
        var checks = new IHealthCheck[]
        {
            new ThrowingHealthCheck(new InvalidOperationException()),
            new StaticHealthCheck(HealthStatus.Healthy),
        };
        var composite = new CompositeHealthCheck(checks);
        var status = await composite.CheckAsync();
        status.Should().Be(HealthStatus.Unhealthy);
    }

    /// <summary>
    /// Cancellation should propagate to the composite check.
    /// </summary>
    /// <returns>A completed task expected to throw.</returns>
    [Fact]
    public async Task CheckAsync_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var composite = new CompositeHealthCheck(
        [
            new CancellingHealthCheck(),
        ]);

        var act = async () => await composite.CheckAsync(cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private sealed class StaticHealthCheck(HealthStatus status) : IHealthCheck
    {
        public Task<HealthStatus> CheckAsync(CancellationToken cancellationToken = default) => Task.FromResult(status);
    }

    private sealed class ThrowingHealthCheck(Exception ex) : IHealthCheck
    {
        public Task<HealthStatus> CheckAsync(CancellationToken cancellationToken = default) => Task.FromException<HealthStatus>(ex);
    }

    private sealed class CancellingHealthCheck : IHealthCheck
    {
        public Task<HealthStatus> CheckAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(HealthStatus.Healthy);
        }
    }
}
