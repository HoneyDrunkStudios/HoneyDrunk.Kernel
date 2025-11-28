using HoneyDrunk.Kernel.Abstractions.Context;
using System.Diagnostics;

namespace HoneyDrunk.Kernel.Telemetry;

/// <summary>
/// Provides the ActivitySource for HoneyDrunk Grid operations and helper methods for creating activities.
/// </summary>
/// <remarks>
/// This class centralizes telemetry activity creation for the Grid. All Grid operations should use this
/// ActivitySource to ensure consistent tracing and OpenTelemetry integration.
/// </remarks>
public static class GridActivitySource
{
    /// <summary>
    /// The ActivitySource name for all HoneyDrunk Grid operations.
    /// </summary>
    public const string SourceName = "HoneyDrunk.Grid";

    /// <summary>
    /// The version of the ActivitySource (matches Kernel version).
    /// </summary>
    public const string Version = "0.3.0";

    /// <summary>
    /// Gets the ActivitySource for HoneyDrunk Grid operations.
    /// </summary>
    public static ActivitySource Instance { get; } = new(SourceName, Version);

    /// <summary>
    /// Starts a new activity with Grid context enrichment.
    /// </summary>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="gridContext">The Grid context to enrich the activity with.</param>
    /// <param name="kind">The activity kind (default: Internal).</param>
    /// <param name="tags">Additional tags to add to the activity.</param>
    /// <returns>The started activity, or null if no listeners are active.</returns>
    public static Activity? StartActivity(
        string operationName,
        IGridContext gridContext,
        ActivityKind kind = ActivityKind.Internal,
        IEnumerable<KeyValuePair<string, object?>>? tags = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentNullException.ThrowIfNull(gridContext);

        var activity = Instance.StartActivity(operationName, kind);
        if (activity is null)
        {
            return null;
        }

        // Enrich with Grid context
        activity.SetTag("hd.correlation_id", gridContext.CorrelationId);
        activity.SetTag("hd.node_id", gridContext.NodeId);
        activity.SetTag("hd.studio_id", gridContext.StudioId);
        activity.SetTag("hd.environment", gridContext.Environment);

        if (gridContext.CausationId is not null)
        {
            activity.SetTag("hd.causation_id", gridContext.CausationId);
        }

        // Add baggage as tags
        foreach (var (key, value) in gridContext.Baggage)
        {
            activity.SetTag($"hd.baggage.{key}", value);
        }

        // Add custom tags
        if (tags is not null)
        {
            foreach (var (key, value) in tags)
            {
                // Activity.SetTag accepts object? but Activity.Tags only exposes string values
                // Convert to string if needed for proper tag storage
                activity.SetTag(key, value?.ToString() ?? string.Empty);
            }
        }

        return activity;
    }

    /// <summary>
    /// Starts a new activity for an HTTP operation with Grid context enrichment.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">The request path.</param>
    /// <param name="gridContext">The Grid context.</param>
    /// <returns>The started activity, or null if no listeners are active.</returns>
    public static Activity? StartHttpActivity(string method, string path, IGridContext gridContext)
    {
        var activity = StartActivity($"HTTP {method} {path}", gridContext, ActivityKind.Server);
        activity?.SetTag("http.method", method);
        activity?.SetTag("http.target", path);
        return activity;
    }

    /// <summary>
    /// Starts a new activity for a database operation with Grid context enrichment.
    /// </summary>
    /// <param name="operationType">The database operation type (e.g., query, command).</param>
    /// <param name="tableName">The table name.</param>
    /// <param name="gridContext">The Grid context.</param>
    /// <returns>The started activity, or null if no listeners are active.</returns>
    public static Activity? StartDatabaseActivity(string operationType, string tableName, IGridContext gridContext)
    {
        var activity = StartActivity($"DB {operationType} {tableName}", gridContext, ActivityKind.Client);
        activity?.SetTag("db.operation", operationType);
        activity?.SetTag("db.table", tableName);
        return activity;
    }

    /// <summary>
    /// Starts a new activity for a message operation with Grid context enrichment.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    /// <param name="destination">The destination (queue/topic name).</param>
    /// <param name="gridContext">The Grid context.</param>
    /// <param name="kind">The activity kind (Producer or Consumer).</param>
    /// <returns>The started activity, or null if no listeners are active.</returns>
    public static Activity? StartMessageActivity(
        string messageType,
        string destination,
        IGridContext gridContext,
        ActivityKind kind = ActivityKind.Producer)
    {
        var activity = StartActivity($"Message {messageType}", gridContext, kind);
        activity?.SetTag("messaging.message_type", messageType);
        activity?.SetTag("messaging.destination", destination);
        return activity;
    }

    /// <summary>
    /// Records an exception on the current activity.
    /// </summary>
    /// <param name="activity">The activity to record the exception on.</param>
    /// <param name="exception">The exception to record.</param>
    public static void RecordException(Activity? activity, Exception exception)
    {
        if (activity is null || exception is null)
        {
            return;
        }

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.SetTag("exception.type", exception.GetType().FullName);
        activity.SetTag("exception.message", exception.Message);
        activity.SetTag("exception.stacktrace", exception.StackTrace);
    }

    /// <summary>
    /// Sets the activity status to OK.
    /// </summary>
    /// <param name="activity">The activity to set the status on.</param>
    public static void SetSuccess(Activity? activity)
    {
        activity?.SetStatus(ActivityStatusCode.Ok);
    }
}
