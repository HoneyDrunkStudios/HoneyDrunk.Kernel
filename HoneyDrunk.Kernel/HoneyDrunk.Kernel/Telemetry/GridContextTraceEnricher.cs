using HoneyDrunk.Kernel.Abstractions.Telemetry;

namespace HoneyDrunk.Kernel.Telemetry;

/// <summary>
/// Enriches traces with Grid context information.
/// </summary>
public sealed class GridContextTraceEnricher : ITraceEnricher
{
    /// <inheritdoc />
    public void Enrich(ITelemetryContext context, IDictionary<string, object?> tags)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(tags, nameof(tags));

        var gridContext = context.GridContext;

        // Add standard Grid tags
        tags[TelemetryTags.CorrelationId] = gridContext.CorrelationId;
        tags[TelemetryTags.NodeId] = gridContext.NodeId;
        tags[TelemetryTags.StudioId] = gridContext.StudioId;
        tags[TelemetryTags.Environment] = gridContext.Environment;

        if (gridContext.CausationId != null)
        {
            tags[TelemetryTags.CausationId] = gridContext.CausationId;
        }

        // Add baggage as tags (with prefix to avoid conflicts)
        foreach (var (key, value) in gridContext.Baggage)
        {
            tags[$"baggage.{key}"] = value;
        }
    }
}
