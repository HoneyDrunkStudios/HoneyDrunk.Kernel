namespace HoneyDrunk.Kernel.Abstractions.Diagnostics;

/// <summary>
/// Provides methods for recording metrics in a testable manner.
/// </summary>
/// <remarks>
/// IMetricsCollector is the Grid-wide abstraction for observability metrics.
/// Implementations should map to appropriate backends (OpenTelemetry, Application Insights, etc.).
/// Use <see cref="Telemetry.TelemetryTags"/> constants for consistent tagging across the Grid.
/// </remarks>
public interface IMetricsCollector
{
    /// <summary>
    /// Records a counter metric that can only increase.
    /// Use for: request counts, error counts, messages processed.
    /// </summary>
    /// <param name="name">The name of the counter metric.</param>
    /// <param name="value">The value to add to the counter (default: 1).</param>
    /// <param name="tags">Optional key-value pairs for metric dimensions. Use <see cref="Telemetry.TelemetryTags"/> constants.</param>
    void RecordCounter(string name, long value = 1, params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Records a histogram metric for value distribution analysis.
    /// Use for: response times, payload sizes, batch sizes.
    /// </summary>
    /// <param name="name">The name of the histogram metric.</param>
    /// <param name="value">The value to record in the histogram.</param>
    /// <param name="tags">Optional key-value pairs for metric dimensions. Use <see cref="Telemetry.TelemetryTags"/> constants.</param>
    void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Records a gauge metric that represents a current value that can go up or down.
    /// Use for: queue depth, active connections, memory usage.
    /// </summary>
    /// <param name="name">The name of the gauge metric.</param>
    /// <param name="value">The current value of the gauge.</param>
    /// <param name="tags">Optional key-value pairs for metric dimensions. Use <see cref="Telemetry.TelemetryTags"/> constants.</param>
    void RecordGauge(string name, double value, params KeyValuePair<string, object?>[] tags);
}
