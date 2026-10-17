namespace HoneyDrunk.Kernel.Abstractions.Telemetry;

/// <summary>
/// Enriches distributed traces with Grid-wide context and metadata.
/// </summary>
/// <remarks>
/// ITraceEnricher implementations add tags, attributes, and baggage to traces
/// based on GridContext, NodeContext, and operation-specific metadata.
/// Enrichers run during trace creation and should be lightweight.
/// </remarks>
public interface ITraceEnricher
{
    /// <summary>
    /// Enriches a trace with context-based tags and attributes.
    /// </summary>
    /// <param name="context">The telemetry context for the current operation.</param>
    /// <param name="tags">Mutable tag collection to enrich.</param>
    void Enrich(ITelemetryContext context, IDictionary<string, object?> tags);
}
