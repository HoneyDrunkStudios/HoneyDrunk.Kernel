using HoneyDrunk.Kernel.Abstractions.Context;

namespace HoneyDrunk.Kernel.Abstractions.Telemetry;

/// <summary>
/// Provides a readonly view of telemetry-relevant context for tracing and logging.
/// </summary>
/// <remarks>
/// ITelemetryContext is derived from IGridContext and provides additional telemetry-specific
/// metadata such as trace IDs, span IDs, and baggage optimized for observability backends.
/// This abstraction allows telemetry enrichers to work without depending on specific
/// distributed tracing implementations (OpenTelemetry, Application Insights, etc.).
/// </remarks>
public interface ITelemetryContext
{
    /// <summary>
    /// Gets the Grid context for this operation.
    /// </summary>
    IGridContext GridContext { get; }

    /// <summary>
    /// Gets the trace ID for distributed tracing.
    /// Maps to W3C Trace Context trace-id or OpenTelemetry TraceId.
    /// </summary>
    string TraceId { get; }

    /// <summary>
    /// Gets the span ID for the current operation.
    /// Maps to W3C Trace Context span-id or OpenTelemetry SpanId.
    /// </summary>
    string SpanId { get; }

    /// <summary>
    /// Gets the parent span ID if this is a child span.
    /// </summary>
    string? ParentSpanId { get; }

    /// <summary>
    /// Gets a value indicating whether gets whether this trace is sampled for collection.
    /// </summary>
    bool IsSampled { get; }

    /// <summary>
    /// Gets telemetry-specific baggage (in addition to GridContext baggage).
    /// Used for propagating vendor-specific metadata.
    /// </summary>
    IReadOnlyDictionary<string, string> TelemetryBaggage { get; }
}
