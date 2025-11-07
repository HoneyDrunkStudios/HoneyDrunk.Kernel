using HoneyDrunk.Kernel.Abstractions.Diagnostics;

namespace HoneyDrunk.Kernel.Diagnostics;

/// <summary>
/// No-op implementation of metrics collection.
/// Real backends (OpenTelemetry, Application Insights, etc.) should be registered by downstream services.
/// </summary>
public sealed class NoOpMetricsCollector : IMetricsCollector
{
    /// <inheritdoc />
    public void RecordCounter(string name, long value = 1, params KeyValuePair<string, object?>[] tags)
    {
    }

    /// <inheritdoc />
    public void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags)
    {
    }

    /// <inheritdoc />
    public void RecordGauge(string name, double value, params KeyValuePair<string, object?>[] tags)
    {
    }
}
