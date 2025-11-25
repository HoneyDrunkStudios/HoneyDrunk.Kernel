namespace HoneyDrunk.Kernel.Abstractions.Context;

/// <summary>
/// Standard header names used for Grid context propagation over HTTP surfaces.
/// </summary>
/// <remarks>
/// These names are stable contracts for downstream surfaces (Web.Rest, Gateway, Edge). They intentionally avoid
/// X- prefixes where a future standard name may be adopted. Keep additions minimal; prefer baggage for ad-hoc keys.
/// </remarks>
public static class GridHeaderNames
{
    /// <summary>
    /// Correlation identifier (ULID or external trace id). Falls back to generated ULID when absent.
    /// </summary>
    public const string CorrelationId = "X-Correlation-Id";

    /// <summary>
    /// Causation identifier referencing the parent correlation id in a causal chain.
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
