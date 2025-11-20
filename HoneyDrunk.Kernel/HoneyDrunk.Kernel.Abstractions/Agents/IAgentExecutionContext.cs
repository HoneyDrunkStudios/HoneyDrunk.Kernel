using HoneyDrunk.Kernel.Abstractions.Context;

namespace HoneyDrunk.Kernel.Abstractions.Agents;

/// <summary>
/// Represents the execution context for an agent operation within the Grid.
/// </summary>
/// <remarks>
/// Agent execution context provides a scoped view of GridContext tailored to
/// the agent's permissions and capabilities. It tracks agent-specific metadata
/// and enforces access controls during agent operations.
/// </remarks>
public interface IAgentExecutionContext
{
    /// <summary>
    /// Gets the agent descriptor for this execution.
    /// </summary>
    IAgentDescriptor Agent { get; }

    /// <summary>
    /// Gets the underlying Grid context (scoped to agent's permissions).
    /// </summary>
    IGridContext GridContext { get; }

    /// <summary>
    /// Gets the operation context for this agent execution.
    /// </summary>
    IOperationContext OperationContext { get; }

    /// <summary>
    /// Gets the timestamp when this execution started.
    /// </summary>
    DateTimeOffset StartedAtUtc { get; }

    /// <summary>
    /// Gets agent-specific execution metadata (tool invocations, tokens used, etc.).
    /// </summary>
    IReadOnlyDictionary<string, object?> ExecutionMetadata { get; }

    /// <summary>
    /// Adds execution metadata for this agent operation.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    void AddMetadata(string key, object? value);

    /// <summary>
    /// Checks if the agent has permission to access a specific resource.
    /// </summary>
    /// <param name="resourceType">The type of resource (e.g., "node", "secret", "data").</param>
    /// <param name="resourceId">The resource identifier.</param>
    /// <returns>True if the agent has access; otherwise false.</returns>
    bool CanAccess(string resourceType, string resourceId);
}
