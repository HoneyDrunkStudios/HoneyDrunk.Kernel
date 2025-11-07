namespace HoneyDrunk.Kernel.Abstractions.Diagnostics;

/// <summary>
/// Represents a counter metric that can only be incremented.
/// </summary>
public interface ICounter
{
    /// <summary>
    /// Increments the counter by the specified amount.
    /// </summary>
    /// <param name="value">The amount to increment by (default is 1).</param>
    void Increment(long value = 1);
}

/// <summary>
/// Represents a gauge metric that can be set to arbitrary values.
/// </summary>
public interface IGauge
{
    /// <summary>
    /// Sets the gauge to the specified value.
    /// </summary>
    /// <param name="value">The value to set.</param>
    void Set(double value);
}

/// <summary>
/// Represents a histogram metric for recording distributions of values.
/// </summary>
public interface IHistogram
{
    /// <summary>
    /// Records a value in the histogram.
    /// </summary>
    /// <param name="value">The value to record.</param>
    void Observe(double value);
}

/// <summary>
/// Provides access to metrics instrumentation.
/// </summary>
public interface IMetrics
{
    /// <summary>
    /// Creates or retrieves a counter metric.
    /// </summary>
    /// <param name="name">The name of the counter.</param>
    /// <param name="tags">Optional tags as key-value pairs.</param>
    /// <returns>A counter metric.</returns>
    ICounter Counter(string name, params (string key, string value)[] tags);

    /// <summary>
    /// Creates or retrieves a gauge metric.
    /// </summary>
    /// <param name="name">The name of the gauge.</param>
    /// <param name="tags">Optional tags as key-value pairs.</param>
    /// <returns>A gauge metric.</returns>
    IGauge Gauge(string name, params (string key, string value)[] tags);

    /// <summary>
    /// Creates or retrieves a histogram metric.
    /// </summary>
    /// <param name="name">The name of the histogram.</param>
    /// <param name="tags">Optional tags as key-value pairs.</param>
    /// <returns>A histogram metric.</returns>
    IHistogram Histogram(string name, params (string key, string value)[] tags);
}
