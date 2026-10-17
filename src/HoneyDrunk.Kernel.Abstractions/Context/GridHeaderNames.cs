namespace HoneyDrunk.Kernel.Abstractions.Context;

/// <summary>
/// Standard header names used for Grid context propagation over HTTP surfaces.
/// </summary>
/// <remarks>
/// These names are stable contracts for downstream surfaces (Web.Rest, Gateway, Edge). They intentionally avoid
/// X- prefixes where a future standard name may be adopted. Keep additions minimal; prefer baggage for ad-hoc keys.
/// Business correlation, operation, and causation IDs are separate from W3C trace and span IDs.
/// </remarks>
public static class GridHeaderNames
{
    /// <summary>
    /// Correlation identifier (ULID or external trace id) - groups all operations in a request tree.
    /// Falls back to a valid incoming trace ID or a generated ULID; never sets the active trace ID.
    /// </summary>
    public const string CorrelationId = "X-Correlation-Id";

    /// <summary>
    /// Operation identifier (ULID) - uniquely identifies this business unit of work.
    /// Independent of the Activity span ID.
    /// </summary>
    public const string OperationId = "X-Operation-Id";

    /// <summary>
    /// Causation identifier referencing the parent operation's OperationId (not CorrelationId).
    /// Forms the business operation chain, independently of the Activity parent span ID.
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
    /// Tenant identifier for multi-tenant isolation.
    /// Identity attribute only - not interpreted or enforced by Kernel.
    /// </summary>
    public const string TenantId = "X-Tenant-Id";

    /// <summary>
    /// Project identifier for project-level organization within a tenant.
    /// Identity attribute only - not interpreted or enforced by Kernel.
    /// </summary>
    public const string ProjectId = "X-Project-Id";

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
