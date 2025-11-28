using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Telemetry;
using System.Diagnostics;

namespace HoneyDrunk.Kernel.Telemetry;

/// <summary>
/// Central telemetry facade for HoneyDrunk Nodes. Provides a single <see cref="ActivitySource"/> and helpers
/// to start activities enriched with Grid / Operation context and registered <see cref="ITraceEnricher"/> tags.
/// </summary>
public static class HoneyDrunkTelemetry
{
    /// <summary>
    /// The shared ActivitySource used by all HoneyDrunk Kernel instrumentation.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new("HoneyDrunk.Kernel");

    /// <summary>
    /// Starts an <see cref="Activity"/> with standardized tagging using the provided contexts.
    /// </summary>
    /// <param name="name">Activity name (logical operation).</param>
    /// <param name="grid">Grid context (required for correlation tags).</param>
    /// <param name="operation">Optional operation context providing operation name / outcome metadata.</param>
    /// <param name="enrichers">Optional registered trace enrichers.</param>
    /// <param name="additionalTags">Optional caller-supplied tags (override default on key collision).</param>
    /// <returns>The started <see cref="Activity"/> or null if not sampled.</returns>
    public static Activity? StartActivity(
        string name,
        IGridContext grid,
        IOperationContext? operation = null,
        IEnumerable<ITraceEnricher>? enrichers = null,
        IReadOnlyDictionary<string, object?>? additionalTags = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(grid);

        var tags = new Dictionary<string, object?>(capacity: 16)
        {
            [TelemetryTags.CorrelationId] = grid.CorrelationId,
            [TelemetryTags.NodeId] = grid.NodeId,
            [TelemetryTags.StudioId] = grid.StudioId,
            [TelemetryTags.Environment] = grid.Environment,
        };

        if (!string.IsNullOrWhiteSpace(grid.CausationId))
        {
            tags[TelemetryTags.CausationId] = grid.CausationId;
        }

        if (operation is not null)
        {
            tags[TelemetryTags.Operation] = operation.OperationName;
            if (operation.IsSuccess.HasValue)
            {
                tags[TelemetryTags.Outcome] = operation.IsSuccess.Value ? "success" : "failure";
            }
        }

        // Allow enrichers to add / override.
        if (enrichers is not null)
        {
            var telemetryContext = new TelemetryContext(grid, traceId: string.Empty, spanId: string.Empty); // Minimal for enricher usage.
            foreach (var enricher in enrichers)
            {
                enricher?.Enrich(telemetryContext, tags);
            }
        }

        if (additionalTags is not null)
        {
            foreach (var kvp in additionalTags)
            {
                tags[kvp.Key] = kvp.Value;
            }
        }

        // Start the activity with tags snapshot.
        var activity = ActivitySource.StartActivity(name, ActivityKind.Internal);
        if (activity is null)
        {
            return null; // Not sampled.
        }

        foreach (var (k, v) in tags)
        {
            activity.SetTag(k, v);
        }

        return activity;
    }
}
