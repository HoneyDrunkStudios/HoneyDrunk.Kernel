using FluentAssertions;
using HoneyDrunk.Kernel.Diagnostics;

namespace HoneyDrunk.Kernel.Tests.Diagnostics;

/// <summary>
/// Tests for <see cref="NoOpMetricsCollector"/>.
/// </summary>
public class NoOpMetricsCollectorTests
{
    /// <summary>
    /// Ensure counter recording is a no-op and does not throw.
    /// </summary>
    [Fact]
    public void RecordCounter_ShouldNotThrow()
    {
        var metrics = new NoOpMetricsCollector();
        var action = () => metrics.RecordCounter("payments.processed", 1, new KeyValuePair<string, object?>("k", "v"));
        action.Should().NotThrow();
    }

    /// <summary>
    /// Ensure histogram recording is a no-op and does not throw.
    /// </summary>
    [Fact]
    public void RecordHistogram_ShouldNotThrow()
    {
        var metrics = new NoOpMetricsCollector();
        var action = () => metrics.RecordHistogram("latency.ms", 42.5, new KeyValuePair<string, object?>("route", "/api"));
        action.Should().NotThrow();
    }

    /// <summary>
    /// Ensure gauge recording is a no-op and does not throw.
    /// </summary>
    [Fact]
    public void RecordGauge_ShouldNotThrow()
    {
        var metrics = new NoOpMetricsCollector();
        var action = () => metrics.RecordGauge("threads.active", 7, new KeyValuePair<string, object?>("pool", "default"));
        action.Should().NotThrow();
    }
}
