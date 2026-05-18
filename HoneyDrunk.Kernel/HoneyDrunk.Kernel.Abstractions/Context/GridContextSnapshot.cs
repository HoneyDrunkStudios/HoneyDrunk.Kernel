using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Kernel.Abstractions.Context;

/// <summary>
/// Abstractions-only initialized <see cref="IGridContext"/> implementation for libraries that need
/// to bootstrap or propagate Grid context without referencing the Kernel runtime package.
/// </summary>
/// <remarks>
/// Use this type for transport envelopes, tests, or library-owned background scopes where a concrete
/// Kernel runtime <c>GridContext</c> is unavailable. App hosts should continue to use the runtime
/// scoped context created by Kernel dependency injection.
/// </remarks>
public sealed class GridContextSnapshot : IGridContext
{
    private readonly Dictionary<string, string> _baggage;

    /// <summary>
    /// Initializes a new instance of the <see cref="GridContextSnapshot"/> class.
    /// </summary>
    /// <param name="nodeId">The executing Node identifier.</param>
    /// <param name="studioId">The owning Studio identifier.</param>
    /// <param name="environment">The execution environment.</param>
    /// <param name="correlationId">The correlation identifier. If omitted, a new ULID is generated.</param>
    /// <param name="causationId">The optional causation identifier.</param>
    /// <param name="tenantId">The tenant identifier. Defaults to <see cref="TenantId.Internal"/>.</param>
    /// <param name="projectId">The optional project identifier.</param>
    /// <param name="baggage">Optional propagated baggage.</param>
    /// <param name="cancellation">The cancellation token for the operation chain.</param>
    /// <param name="createdAtUtc">The UTC creation time. Defaults to now.</param>
    public GridContextSnapshot(
        string nodeId,
        string studioId,
        string environment,
        string? correlationId = null,
        string? causationId = null,
        TenantId? tenantId = null,
        string? projectId = null,
        IReadOnlyDictionary<string, string>? baggage = null,
        CancellationToken cancellation = default,
        DateTimeOffset? createdAtUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(studioId);
        ArgumentException.ThrowIfNullOrWhiteSpace(environment);

        NodeId = nodeId;
        StudioId = studioId;
        Environment = environment;
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? Ulid.NewUlid().ToString() : correlationId;
        CausationId = causationId;
        TenantId = tenantId ?? TenantId.Internal;
        ProjectId = projectId;
        Cancellation = cancellation;
        CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow;
        _baggage = baggage is null ? [] : new Dictionary<string, string>(baggage);
    }

    /// <inheritdoc />
    public bool IsInitialized => true;

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
    public TenantId TenantId { get; }

    /// <inheritdoc />
    public string? ProjectId { get; }

    /// <inheritdoc />
    public CancellationToken Cancellation { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Baggage => _baggage;

    /// <inheritdoc />
    public DateTimeOffset CreatedAtUtc { get; }

    /// <inheritdoc />
    public void AddBaggage(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        _baggage[key] = value;
    }
}
