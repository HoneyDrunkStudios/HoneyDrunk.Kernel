using HoneyDrunk.Kernel.Abstractions.Context;

namespace HoneyDrunk.Kernel.Context;

/// <summary>
/// Default implementation of IGridContext for Grid-wide operation tracking.
/// </summary>
/// <remarks>
/// <para>
/// GridContext is a mutable, scoped object owned by DI. Exactly one instance exists per scope.
/// Middleware and mappers enrich this instance via <see cref="Initialize"/> rather than creating
/// new instances.
/// </para>
/// <para>
/// The context starts in an uninitialized state. Accessing properties before initialization
/// throws <see cref="InvalidOperationException"/>. This ensures misconfiguration fails loudly.
/// </para>
/// </remarks>
public sealed class GridContext : IGridContext
{
    private readonly Dictionary<string, string> _baggage = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="GridContext"/> class in an uninitialized state.
    /// </summary>
    /// <param name="nodeId">The Node identifier (from NodeContext).</param>
    /// <param name="studioId">The Studio identifier (from NodeContext).</param>
    /// <param name="environment">The environment name (from NodeContext).</param>
    /// <remarks>
    /// The context is created with Node-level identity but requires explicit initialization
    /// via <see cref="Initialize"/> before use. This is typically done by middleware.
    /// </remarks>
    public GridContext(string nodeId, string studioId, string environment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(studioId);
        ArgumentException.ThrowIfNullOrWhiteSpace(environment);

        NodeId = nodeId;
        StudioId = studioId;
        Environment = environment;
    }

    /// <inheritdoc />
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this context has been disposed.
    /// </summary>
    /// <remarks>
    /// When true, any attempt to access context properties will throw
    /// <see cref="ObjectDisposedException"/>. This is used to detect
    /// fire-and-forget patterns that incorrectly hold references to context.
    /// </remarks>
    public bool IsDisposed { get; private set; }

    /// <inheritdoc />
    public string CorrelationId
    {
        get
        {
            ThrowIfNotInitialized();
            return field!;
        }

        private set;
    }

    /// <inheritdoc />
    public string? CausationId
    {
        get
        {
            ThrowIfNotInitialized();
            return field;
        }

        private set;
    }

    /// <inheritdoc />
    public string NodeId
    {
        get
        {
            ThrowIfNotInitialized();
            return field;
        }
    }

    /// <inheritdoc />
    public string StudioId
    {
        get
        {
            ThrowIfNotInitialized();
            return field;
        }
    }

    /// <inheritdoc />
    public string Environment
    {
        get
        {
            ThrowIfNotInitialized();
            return field;
        }
    }

    /// <inheritdoc />
    public string? TenantId
    {
        get
        {
            ThrowIfNotInitialized();
            return field;
        }

        private set;
    }

    /// <inheritdoc />
    public string? ProjectId
    {
        get
        {
            ThrowIfNotInitialized();
            return field;
        }

        private set;
    }

    /// <inheritdoc />
    public CancellationToken Cancellation
    {
        get
        {
            ThrowIfNotInitialized();
            return field;
        }

        private set;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Baggage
    {
        get
        {
            ThrowIfNotInitialized();
            return _baggage;
        }
    }

    /// <inheritdoc />
    public DateTimeOffset CreatedAtUtc
    {
        get
        {
            ThrowIfNotInitialized();
            return field;
        }

        private set;
    }

    /// <summary>
    /// Initializes the context with request-specific data.
    /// </summary>
    /// <param name="correlationId">The correlation identifier (trace-id).</param>
    /// <param name="causationId">Optional causation identifier (parent-span-id).</param>
    /// <param name="tenantId">Optional tenant identifier.</param>
    /// <param name="projectId">Optional project identifier.</param>
    /// <param name="baggage">Optional baggage dictionary.</param>
    /// <param name="cancellation">Optional cancellation token.</param>
    /// <exception cref="InvalidOperationException">Thrown if context is already initialized or disposed.</exception>
    public void Initialize(
        string correlationId,
        string? causationId = null,
        string? tenantId = null,
        string? projectId = null,
        IReadOnlyDictionary<string, string>? baggage = null,
        CancellationToken cancellation = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        ThrowIfDisposed();

        if (IsInitialized)
        {
            throw new InvalidOperationException(
                "GridContext has already been initialized. " +
                "A GridContext can only be initialized once per scope. " +
                "Ensure middleware or context setup runs exactly once.");
        }

        CorrelationId = correlationId;
        CausationId = causationId;
        TenantId = tenantId;
        ProjectId = projectId;
        Cancellation = cancellation;
        CreatedAtUtc = DateTimeOffset.UtcNow;

        if (baggage != null)
        {
            foreach (var kvp in baggage)
            {
                _baggage[kvp.Key] = kvp.Value;
            }
        }

        IsInitialized = true;
    }

    /// <inheritdoc />
    public void AddBaggage(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        ThrowIfNotInitialized();
        ThrowIfDisposed();

        _baggage[key] = value;
    }

    /// <summary>
    /// Marks this context as disposed, preventing further use.
    /// </summary>
    /// <remarks>
    /// This is called when the owning scope ends. Any subsequent access throws
    /// <see cref="ObjectDisposedException"/> to catch fire-and-forget patterns
    /// that incorrectly assume context survives beyond its scope.
    /// </remarks>
    internal void MarkDisposed()
    {
        IsDisposed = true;
    }

    private void ThrowIfNotInitialized()
    {
        ThrowIfDisposed();

        if (!IsInitialized)
        {
            throw new InvalidOperationException(
                "GridContext has not been initialized. " +
                "Ensure UseGridContext() middleware is registered in the pipeline, " +
                "or that context initialization occurs before accessing context properties. " +
                "For background services, use explicit context creation via mappers.");
        }
    }

    private void ThrowIfDisposed()
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException(
                nameof(GridContext),
                "GridContext has been disposed because its owning scope has ended. Context cannot be used after scope disposal. Fire-and-forget patterns that rely on ambient context are not supported. Background work must explicitly create and own its context.");
        }
    }
}
