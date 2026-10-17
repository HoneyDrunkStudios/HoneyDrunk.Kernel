using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Telemetry;

namespace HoneyDrunk.Kernel.Telemetry;

/// <summary>
/// Default implementation of ITelemetryContext.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TelemetryContext"/> class.
/// </remarks>
/// <param name="gridContext">The Grid context.</param>
/// <param name="traceId">The trace identifier.</param>
/// <param name="spanId">The span identifier.</param>
/// <param name="parentSpanId">The parent span identifier.</param>
/// <param name="isSampled">Whether the trace is sampled.</param>
/// <param name="telemetryBaggage">Optional telemetry baggage.</param>
public sealed class TelemetryContext(
    IGridContext gridContext,
    string traceId,
    string spanId,
    string? parentSpanId = null,
    bool isSampled = true,
    IReadOnlyDictionary<string, string>? telemetryBaggage = null) : ITelemetryContext
{
    /// <inheritdoc />
    public IGridContext GridContext { get; } = gridContext ?? throw new ArgumentNullException(nameof(gridContext));

    /// <inheritdoc />
    public string TraceId { get; } = traceId ?? throw new ArgumentNullException(nameof(traceId));

    /// <inheritdoc />
    public string SpanId { get; } = spanId ?? throw new ArgumentNullException(nameof(spanId));

    /// <inheritdoc />
    public string? ParentSpanId { get; } = parentSpanId;

    /// <inheritdoc />
    public bool IsSampled { get; } = isSampled;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> TelemetryBaggage { get; } = telemetryBaggage ?? new Dictionary<string, string>();
}
