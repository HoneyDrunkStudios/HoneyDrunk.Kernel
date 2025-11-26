using HoneyDrunk.Kernel.Abstractions.Context;

namespace HoneyDrunk.Kernel.Context.Mappers;

/// <summary>
/// Maps background job context to GridContext.
/// </summary>
/// <remarks>
/// Used for scheduled jobs, background tasks, and batch processing.
/// Creates context from job metadata or generates new correlation IDs for isolated jobs.
/// </remarks>
public sealed class JobContextMapper
{
    private readonly string _nodeId;
    private readonly string _studioId;
    private readonly string _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobContextMapper"/> class.
    /// </summary>
    /// <param name="nodeId">The Node identifier.</param>
    /// <param name="studioId">The Studio identifier.</param>
    /// <param name="environment">The environment name.</param>
    public JobContextMapper(string nodeId, string studioId, string environment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId, nameof(nodeId));
        ArgumentException.ThrowIfNullOrWhiteSpace(studioId, nameof(studioId));
        ArgumentException.ThrowIfNullOrWhiteSpace(environment, nameof(environment));

        _nodeId = nodeId;
        _studioId = studioId;
        _environment = environment;
    }

    /// <summary>
    /// Creates a GridContext for a background job.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="jobType">The job type/name.</param>
    /// <param name="parameters">Optional job parameters to include as baggage.</param>
    /// <param name="cancellationToken">Cancellation token for job execution.</param>
    /// <returns>A GridContext for the background job.</returns>
    public IGridContext MapFromJob(
        string jobId,
        string jobType,
        IReadOnlyDictionary<string, string>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId, nameof(jobId));
        ArgumentException.ThrowIfNullOrWhiteSpace(jobType, nameof(jobType));

        // Use job ID as correlation ID for tracking all work related to this job
        var correlationId = jobId;
        var operationId = Ulid.NewUlid().ToString(); // New span for this job execution

        var baggage = new Dictionary<string, string>
        {
            ["job-type"] = jobType,
            ["job-id"] = jobId,
        };

        // Add job parameters as baggage
        if (parameters != null)
        {
            foreach (var (key, value) in parameters)
            {
                baggage[$"job-param-{key}"] = value;
            }
        }

        return new GridContext(
            correlationId: correlationId,
            operationId: operationId,
            nodeId: _nodeId,
            studioId: _studioId,
            environment: _environment,
            causationId: null, // Jobs typically don't have a causation ID unless triggered by another operation
            baggage: baggage,
            cancellation: cancellationToken);
    }

    /// <summary>
    /// Creates a GridContext for a scheduled/recurring job.
    /// </summary>
    /// <param name="jobName">The scheduled job name.</param>
    /// <param name="executionTime">The scheduled execution time.</param>
    /// <param name="cancellationToken">Cancellation token for job execution.</param>
    /// <returns>A GridContext for the scheduled job.</returns>
    public IGridContext MapFromScheduledJob(
        string jobName,
        DateTimeOffset executionTime,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobName, nameof(jobName));

        // Generate unique correlation ID for each execution
        var correlationId = Ulid.NewUlid().ToString();
        var operationId = Ulid.NewUlid().ToString(); // New span for this scheduled job execution

        var baggage = new Dictionary<string, string>
        {
            ["job-type"] = "scheduled",
            ["job-name"] = jobName,
            ["scheduled-time"] = executionTime.ToString("O"),
        };

        return new GridContext(
            correlationId: correlationId,
            operationId: operationId,
            nodeId: _nodeId,
            studioId: _studioId,
            environment: _environment,
            causationId: null,
            baggage: baggage,
            cancellation: cancellationToken);
    }
}
