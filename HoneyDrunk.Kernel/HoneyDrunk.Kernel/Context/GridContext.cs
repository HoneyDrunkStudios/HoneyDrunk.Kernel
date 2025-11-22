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
    /// <param name="correlationId">The correlation identifier.</param>
    /// <param name="nodeId">The Node identifier.</param>
    /// <param name="studioId">The Studio identifier.</param>
    /// <param name="environment">The environment name.</param>
    /// <param name="causationId">Optional causation identifier.</param>
    /// <param name="baggage">Optional baggage dictionary.</param>
    /// <param name="createdAtUtc">Optional creation timestamp; defaults to current UTC time.</param>
    /// <param name="cancellation">Optional cancellation token.</param>
    public GridContext(
        string correlationId,
        string nodeId,
        string studioId,
        string environment,
        string? causationId = null,
        IReadOnlyDictionary<string, string>? baggage = null,
        DateTimeOffset? createdAtUtc = null,
        CancellationToken cancellation = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(studioId);
        ArgumentException.ThrowIfNullOrWhiteSpace(environment);

        CorrelationId = correlationId;
        CausationId = causationId;
        NodeId = nodeId;
        StudioId = studioId;
        Environment = environment;
        Cancellation = cancellation;
        CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow;
        _baggage = baggage != null ? new Dictionary<string, string>(baggage) : [];
    }

    /// <inheritdoc />
    public string CorrelationId { get; }

    /// <inheritdoc />
    public string? CausationId { get; }

    /// <inheritdoc />
    public string NodeId { get; }

    /// <inheritdoc />
    public string StudioId { get; }

    /// <inheritdoc />
    public string Environment { get; }

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
        return new GridContext(
            correlationId: Ulid.NewUlid().ToString(),
            nodeId: nodeId ?? NodeId,
            studioId: StudioId,
            environment: Environment,
            causationId: CorrelationId,
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
            nodeId: NodeId,
            studioId: StudioId,
            environment: Environment,
            causationId: CausationId,
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
