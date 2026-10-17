using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Kernel.Context.Mappers;

/// <summary>
/// Initializes GridContext instances for background job execution.
/// </summary>
/// <remarks>
/// <para>
/// Used for scheduled jobs, background tasks, and batch processing where HTTP context is not available.
/// Background services must explicitly create and initialize their context using this mapper.
/// </para>
/// <para>
/// <strong>Usage:</strong> Background services should resolve a new scope, get the GridContext from that scope,
/// and use this mapper to initialize it before any work begins.
/// </para>
/// </remarks>
public static class JobContextMapper
{
    /// <summary>
    /// Initializes a GridContext for a background job.
    /// </summary>
    /// <param name="context">The GridContext to initialize.</param>
    /// <param name="jobId">The job identifier (used as correlation ID).</param>
    /// <param name="jobType">The job type/name.</param>
    /// <param name="parameters">Optional job parameters to include as baggage.</param>
    /// <param name="cancellationToken">Cancellation token for job execution.</param>
    public static void InitializeForJob(
        GridContext context,
        string jobId,
        string jobType,
        IReadOnlyDictionary<string, string>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);
        ArgumentException.ThrowIfNullOrWhiteSpace(jobType);

        var baggage = new Dictionary<string, string>
        {
            ["job-type"] = jobType,
            ["job-id"] = jobId,
        };

        if (parameters != null)
        {
            foreach (var (key, value) in parameters)
            {
                baggage[$"job-param-{key}"] = value;
            }
        }

        // Use job ID as correlation ID for tracking all work related to this job
        context.Initialize(
            correlationId: jobId,
            causationId: null,
            tenantId: null,
            projectId: null,
            baggage: baggage,
            cancellation: cancellationToken);
    }

    /// <summary>
    /// Initializes a GridContext for a scheduled/recurring job.
    /// </summary>
    /// <param name="context">The GridContext to initialize.</param>
    /// <param name="jobName">The scheduled job name.</param>
    /// <param name="executionTime">The scheduled execution time.</param>
    /// <param name="cancellationToken">Cancellation token for job execution.</param>
    public static void InitializeForScheduledJob(
        GridContext context,
        string jobName,
        DateTimeOffset executionTime,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(jobName);

        var baggage = new Dictionary<string, string>
        {
            ["job-type"] = "scheduled",
            ["job-name"] = jobName,
            ["scheduled-time"] = executionTime.ToString("O"),
        };

        // Generate unique correlation ID for each execution
        context.Initialize(
            correlationId: Ulid.NewUlid().ToString(),
            causationId: null,
            tenantId: null,
            projectId: null,
            baggage: baggage,
            cancellation: cancellationToken);
    }

    /// <summary>
    /// Initializes a GridContext from serialized job metadata (for propagated jobs).
    /// </summary>
    /// <param name="context">The GridContext to initialize.</param>
    /// <param name="metadata">The job metadata containing serialized context values.</param>
    /// <param name="cancellationToken">Cancellation token for job execution.</param>
    public static void InitializeFromMetadata(
        GridContext context,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(metadata);

        var correlationId = metadata.TryGetValue(GridHeaderNames.CorrelationId, out var corr) ? corr : Ulid.NewUlid().ToString();
        var causationId = metadata.TryGetValue(GridHeaderNames.CausationId, out var cause) ? cause : null;
        var tenantId = metadata.TryGetValue(GridHeaderNames.TenantId, out var tenant)
            ? ParseTenantId(tenant)
            : (TenantId?)null;
        var projectId = metadata.TryGetValue(GridHeaderNames.ProjectId, out var project) ? project : null;

        // Extract baggage from prefixed keys
        var baggage = new Dictionary<string, string>();
        foreach (var kvp in metadata.Where(static kvp => kvp.Key.StartsWith(GridHeaderNames.BaggagePrefix, StringComparison.OrdinalIgnoreCase)))
        {
            var key = kvp.Key[GridHeaderNames.BaggagePrefix.Length..];
            baggage[key] = kvp.Value;
        }

        context.Initialize(
            correlationId: correlationId,
            causationId: causationId,
            tenantId: tenantId,
            projectId: projectId,
            baggage: baggage,
            cancellation: cancellationToken);
    }

    private static TenantId ParseTenantId(string value)
    {
        if (TenantId.TryParse(value, out var tenantId))
        {
            return tenantId;
        }

        throw new FormatException($"Metadata {GridHeaderNames.TenantId} must be a valid ULID.");
    }
}
