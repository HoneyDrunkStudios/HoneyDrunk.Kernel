using HoneyDrunk.Kernel.Abstractions.Context;

namespace HoneyDrunk.Kernel.Context;

/// <summary>
/// Default implementation of IGridContext for Grid-wide operation tracking.
/// </summary>
public sealed class GridContext : IGridContext
{
    private readonly Dictionary<string, string> _baggage;

    /// <summary>
    /// Initializes a new instance of the <see cref="GridContext"/> class.
    /// </summary>
    /// <param name="correlationId">The correlation identifier (trace-id).</param>
    /// <param name="operationId">The operation identifier (span-id).</param>
    /// <param name="nodeId">The Node identifier.</param>
    /// <param name="studioId">The Studio identifier.</param>
    /// <param name="environment">The environment name.</param>
    /// <param name="causationId">Optional causation identifier (parent-span-id).</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenant scenarios.</param>
    /// <param name="projectId">Optional project identifier within a tenant.</param>
    /// <param name="baggage">Optional baggage dictionary.</param>
    /// <param name="createdAtUtc">Optional creation timestamp; defaults to current UTC time.</param>
    /// <param name="cancellation">Optional cancellation token.</param>
    public GridContext(
        string correlationId,
        string operationId,
        string nodeId,
        string studioId,
        string environment,
        string? causationId = null,
        string? tenantId = null,
        string? projectId = null,
        IReadOnlyDictionary<string, string>? baggage = null,
        DateTimeOffset? createdAtUtc = null,
        CancellationToken cancellation = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(studioId);
        ArgumentException.ThrowIfNullOrWhiteSpace(environment);

        CorrelationId = correlationId;
        OperationId = operationId;
        CausationId = causationId;
        NodeId = nodeId;
        StudioId = studioId;
        Environment = environment;
        TenantId = tenantId;
        ProjectId = projectId;
        Cancellation = cancellation;
        CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow;
        _baggage = baggage != null ? new Dictionary<string, string>(baggage) : [];
    }

    /// <inheritdoc />
    public string CorrelationId { get; }

    /// <inheritdoc />
    public string OperationId { get; }

    /// <inheritdoc />
    public string? CausationId { get; }

    /// <inheritdoc />
    public string NodeId { get; }

    /// <inheritdoc />
    public string StudioId { get; }

    /// <inheritdoc />
    public string Environment { get; }

    /// <inheritdoc />
    public string? TenantId { get; }

    /// <inheritdoc />
    public string? ProjectId { get; }

    /// <inheritdoc />
    public CancellationToken Cancellation { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Baggage => _baggage;

    /// <inheritdoc />
    public DateTimeOffset CreatedAtUtc { get; }

    /// <inheritdoc />
    public IDisposable BeginScope()
    {
        // AsyncLocal-based scope management would go here
        // For now, return a no-op disposable
        return new NoOpDisposable();
    }

    /// <inheritdoc />
    public IGridContext CreateChildContext(string? nodeId = null)
    {
        // Three-ID Model:
        // - CorrelationId: Same (constant across trace)
        // - OperationId: New ULID (unique span)
        // - CausationId: Current OperationId (parent span)
        // - TenantId/ProjectId: Preserved from parent
        return new GridContext(
            correlationId: CorrelationId,
            operationId: Ulid.NewUlid().ToString(),
            nodeId: nodeId ?? NodeId,
            studioId: StudioId,
            environment: Environment,
            causationId: OperationId,
            tenantId: TenantId,
            projectId: ProjectId,
            baggage: _baggage,
            cancellation: Cancellation);
    }

    /// <inheritdoc />
    public IGridContext WithBaggage(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var newBaggage = new Dictionary<string, string>(_baggage)
        {
            [key] = value,
        };

        return new GridContext(
            correlationId: CorrelationId,
            operationId: OperationId,
            nodeId: NodeId,
            studioId: StudioId,
            environment: Environment,
            causationId: CausationId,
            tenantId: TenantId,
            projectId: ProjectId,
            baggage: newBaggage,
            cancellation: Cancellation,
            createdAtUtc: CreatedAtUtc);
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
