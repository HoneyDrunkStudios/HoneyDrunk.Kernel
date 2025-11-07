using HoneyDrunk.Kernel.Abstractions.Diagnostics;

namespace HoneyDrunk.Kernel.Diagnostics;

/// <summary>
/// No-op implementation of IMetrics for testing and fallback scenarios.
/// </summary>
public sealed class NoOpMetrics : IMetrics
{
    /// <inheritdoc />
    public ICounter Counter(string name, params (string key, string value)[] tags) => new NoOpCounter();

    /// <inheritdoc />
    public IGauge Gauge(string name, params (string key, string value)[] tags) => new NoOpGauge();

    /// <inheritdoc />
    public IHistogram Histogram(string name, params (string key, string value)[] tags) => new NoOpHistogram();

    private sealed class NoOpCounter : ICounter
    {
        public void Increment(long value = 1)
        {
        }
    }

    private sealed class NoOpGauge : IGauge
    {
        public void Set(double value)
        {
        }
    }

    private sealed class NoOpHistogram : IHistogram
    {
        public void Observe(double value)
        {
        }
    }
}
