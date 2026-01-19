namespace HoneyDrunk.Kernel.Abstractions.Context;

/// <summary>
/// Represents the execution context for a Grid operation, providing correlation, causation,
/// Node identity, and propagated metadata across the entire HoneyDrunk.OS Grid.
/// </summary>
/// <remarks>
/// <para>
/// GridContext is the fundamental OS-level primitive that flows through every operation
/// in the HoneyDrunk ecosystem. It carries:
/// </para>
/// <list type="bullet">
/// <item>Correlation ID (Trace ID): Groups related operations across Nodes (constant per request)</item>
/// <item>Causation ID (Parent Span ID): Tracks which operation triggered this one</item>
/// <item>Node Identity: Which Node is executing this operation</item>
/// <item>Studio Context: Which Studio owns this execution</item>
/// <item>Environment: Which environment (production, staging, development, etc.)</item>
/// <item>Baggage: User-defined metadata that propagates with the context</item>
/// <item>Cancellation: Cooperative cancellation for the entire operation chain</item>
/// </list>
/// <para>
/// The CorrelationId and CausationId align with W3C Trace Context and OpenTelemetry standards.
/// OperationId (Span ID) is owned by IOperationContext, not GridContext.
/// </para>
/// <para>
/// <strong>Ownership model:</strong> Exactly one GridContext instance exists per DI scope.
/// That instance is created by DI and enriched by middleware/mappers. IGridContextAccessor
/// provides ambient access to the same scoped instance. Code must not create additional
/// GridContext instances within a scope.
/// </para>
/// </remarks>
public interface IGridContext
{
    /// <summary>
    /// Gets a value indicating whether this context has been initialized with request-specific data.
    /// </summary>
    /// <remarks>
    /// A context is considered initialized after middleware or a mapper has populated it with
    /// correlation, causation, and other request-specific identifiers. Accessing properties
    /// on an uninitialized context throws <see cref="InvalidOperationException"/>.
    /// </remarks>
    bool IsInitialized { get; }

    /// <summary>
    /// Gets the correlation identifier (trace ID) that groups related operations across the Grid.
    /// This ID remains constant as work flows through multiple Nodes within a single user request.
    /// Created once at the edge and propagated unchanged through all downstream operations.
    /// Maps to W3C traceparent trace-id and OpenTelemetry trace_id.
    /// </summary>
    string CorrelationId { get; }

    /// <summary>
    /// Gets the causation identifier (parent span ID) indicating which operation triggered this one.
    /// Points to the parent operation's OperationId. Null for root operations.
    /// Maps to W3C traceparent parent-id and OpenTelemetry parent_span_id.
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
    /// Gets the tenant identifier for multi-tenant isolation.
    /// This is an identity attribute ONLY - Kernel does not interpret, authorize, or enforce it.
    /// Used for propagation across nodes, logs, telemetry, and tracing.
    /// Null if the operation is not tenant-scoped.
    /// </summary>
    string? TenantId { get; }

    /// <summary>
    /// Gets the project identifier for project-level organization within a tenant.
    /// This is an identity attribute ONLY - Kernel does not interpret, authorize, or enforce it.
    /// Used for propagation across nodes, logs, telemetry, and tracing.
    /// Null if the operation is not project-scoped.
    /// </summary>
    string? ProjectId { get; }

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
    /// Adds or updates baggage that will propagate to downstream operations.
    /// Mutates the current context instance.
    /// </summary>
    /// <param name="key">The baggage key.</param>
    /// <param name="value">The baggage value.</param>
    void AddBaggage(string key, string value);
}
