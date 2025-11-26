namespace HoneyDrunk.Kernel.Abstractions.Context;

/// <summary>
/// Standard header names used for Grid context propagation over HTTP surfaces.
/// </summary>
/// <remarks>
/// These names are stable contracts for downstream surfaces (Web.Rest, Gateway, Edge). They intentionally avoid
/// X- prefixes where a future standard name may be adopted. Keep additions minimal; prefer baggage for ad-hoc keys.
/// The three-ID model (Correlation, Operation, Causation) maps directly to W3C Trace Context (trace-id, span-id, parent-id).
/// </remarks>
public static class GridHeaderNames
{
    /// <summary>
    /// Correlation identifier (ULID or external trace id) - groups all operations in a request tree.
    /// Falls back to generated ULID when absent. Maps to W3C traceparent trace-id.
    /// </summary>
    public const string CorrelationId = "X-Correlation-Id";

    /// <summary>
    /// Operation identifier (ULID) - uniquely identifies this unit of work (span) within the trace.
    /// Maps to W3C traceparent span-id.
    /// </summary>
    public const string OperationId = "X-Operation-Id";

    /// <summary>
    /// Causation identifier referencing the parent operation's OperationId (not CorrelationId).
    /// Forms parent-child chain for distributed tracing. Maps to W3C traceparent parent-id.
    /// </summary>
    public const string CausationId = "X-Causation-Id";

    /// <summary>
    /// Studio identifier owning the execution (multi-studio / multi-workspace isolation).
    /// </summary>
    public const string StudioId = "X-Studio-Id";

    /// <summary>
    /// Node identifier executing the request (echoed on responses).
    /// </summary>
    public const string NodeId = "X-Node-Id";

    /// <summary>
    /// Environment identifier (e.g., production, staging, development).
    /// </summary>
    public const string Environment = "X-Environment";

    /// <summary>
    /// W3C traceparent header (for interoperability) used as secondary correlation source.
    /// Format: version-trace_id-span_id-trace_flags (e.g., "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01").
    /// </summary>
    public const string TraceParent = "traceparent";

    /// <summary>
    /// W3C baggage header containing comma-separated key=value pairs.
    /// </summary>
    public const string Baggage = "baggage";

    /// <summary>
    /// Prefix for custom baggage headers (e.g., X-Baggage-TenantId).
    /// </summary>
    public const string BaggagePrefix = "X-Baggage-";
}
