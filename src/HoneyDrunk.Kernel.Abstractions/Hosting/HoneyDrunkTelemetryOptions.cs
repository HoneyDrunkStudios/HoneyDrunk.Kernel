namespace HoneyDrunk.Kernel.Abstractions.Hosting;

/// <summary>
/// Telemetry-related bootstrap configuration for a HoneyDrunk Node.
/// </summary>
public sealed class HoneyDrunkTelemetryOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether distributed tracing instrumentation is enabled.
    /// </summary>
    public bool EnableTracing { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether metrics collection helpers are enabled.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether log correlation scopes are enabled.
    /// </summary>
    public bool EnableLogCorrelation { get; set; } = true;

    /// <summary>
    /// Gets or sets the trace sampling ratio (0.0 - 1.0).
    /// </summary>
    public double TraceSamplingRatio { get; set; } = 1.0;
}
