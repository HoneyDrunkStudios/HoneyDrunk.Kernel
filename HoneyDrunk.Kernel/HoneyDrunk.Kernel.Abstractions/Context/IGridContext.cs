namespace HoneyDrunk.Kernel.Abstractions.Context;

/// <summary>
/// Represents the execution context for a Grid operation, providing correlation, causation,
/// Node identity, and propagated metadata across the entire HoneyDrunk.OS Grid.
/// </summary>
/// <remarks>
/// GridContext is the fundamental OS-level primitive that flows through every operation
/// in the HoneyDrunk ecosystem. It carries:
/// - Correlation ID: Groups related operations across Nodes (stays constant per user request)
/// - Causation ID: Tracks which operation triggered this one (parent-child chain)
/// - Node Identity: Which Node is executing this operation
/// - Studio Context: Which Studio owns this execution
/// - Environment: Which environment (production, staging, development, etc.)
/// - Baggage: User-defined metadata that propagates with the context
/// - Cancellation: Cooperative cancellation for the entire operation chain.
/// </remarks>
public interface IGridContext
{
    /// <summary>
    /// Gets the correlation identifier that groups related operations across the Grid.
    /// This ID remains constant as work flows through multiple Nodes within a single user request.
    /// Created once at the edge and propagated unchanged through all downstream operations.
    /// </summary>
    string CorrelationId { get; }

    /// <summary>
    /// Gets the causation identifier indicating which operation triggered this one.
    /// Forms a parent-child chain: if Operation A triggers Operation B, then B.CausationId points to A's identifier.
    /// Null for root operations (no parent).
    /// </summary>
    string? CausationId { get; }

    /// <summary>
    /// Gets the identifier of the Node currently executing this operation.
    /// Uses kebab-case format (e.g., "kernel", "payment-service", "auth-gateway").
    /// </summary>
    string NodeId { get; }

    /// <summary>
    /// Gets the Studio identifier that owns this execution context.
    /// Identifies which logical Studio (organization/tenant) this execution belongs to.
    /// </summary>
    string StudioId { get; }

    /// <summary>
    /// Gets the environment in which this operation is executing.
    /// Uses kebab-case format (e.g., "production", "staging", "development", "local").
    /// </summary>
    string Environment { get; }

    /// <summary>
    /// Gets the cancellation token for the current operation and all downstream operations.
    /// </summary>
    CancellationToken Cancellation { get; }

    /// <summary>
    /// Gets the baggage (key-value pairs) propagated with this context.
    /// Baggage flows across Node boundaries and should be kept minimal.
    /// </summary>
    IReadOnlyDictionary<string, string> Baggage { get; }

    /// <summary>
    /// Gets the UTC timestamp when this context was created.
    /// </summary>
    DateTimeOffset CreatedAtUtc { get; }

    /// <summary>
    /// Begins a new logical scope for the context.
    /// Used for nested operations within the same Node.
    /// </summary>
    /// <returns>A disposable object that ends the scope when disposed.</returns>
    IDisposable BeginScope();

    /// <summary>
    /// Creates a new child context for a downstream operation.
    /// The current context's CorrelationId is preserved (stays constant for the entire request tree).
    /// The current context's CorrelationId becomes the CausationId of the child (forming parent-child chain).
    /// </summary>
    /// <param name="nodeId">Optional Node ID if crossing Node boundaries; if null, uses current NodeId.</param>
    /// <returns>A new GridContext with the same CorrelationId and the current context as its cause.</returns>
    IGridContext CreateChildContext(string? nodeId = null);

    /// <summary>
    /// Adds or updates baggage that will propagate to downstream operations.
    /// Returns a new context instance with the updated baggage.
    /// </summary>
    /// <param name="key">The baggage key.</param>
    /// <param name="value">The baggage value.</param>
    /// <returns>A new GridContext with the updated baggage.</returns>
    IGridContext WithBaggage(string key, string value);
}
