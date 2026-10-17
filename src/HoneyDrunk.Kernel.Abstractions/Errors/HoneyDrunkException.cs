using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Kernel.Abstractions.Errors;

/// <summary>
/// Base exception for HoneyDrunk Kernel semantics. Carries Grid identity primitives for cross-process correlation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="HoneyDrunkException"/> class with a message and optional identity metadata.
/// </remarks>
/// <param name="message">Human-readable error message.</param>
/// <param name="correlationId">Optional correlation identifier for the failing operation.</param>
/// <param name="errorCode">Optional structured error code.</param>
/// <param name="nodeId">Optional Node identifier where error originated.</param>
/// <param name="environmentId">Optional Environment identifier.</param>
/// <param name="innerException">Optional inner exception causing this error.</param>
public class HoneyDrunkException(
    string message,
    CorrelationId? correlationId = null,
    ErrorCode? errorCode = null,
    NodeId? nodeId = null,
    EnvironmentId? environmentId = null,
    Exception? innerException = null) : Exception(message, innerException)
{
    /// <summary>
    /// Gets the correlation identifier for the failing operation (ULID) if available.
    /// </summary>
    public CorrelationId? CorrelationId { get; } = correlationId;

    /// <summary>
    /// Gets the structured error code used for classification (e.g. "validation.input", "dependency.failure").
    /// </summary>
    public ErrorCode? ErrorCode { get; } = errorCode;

    /// <summary>
    /// Gets the Node identity where exception originated (kebab-case).
    /// </summary>
    public NodeId? NodeId { get; } = nodeId;

    /// <summary>
    /// Gets the Environment identity where exception originated (e.g. production, staging).
    /// </summary>
    public EnvironmentId? EnvironmentId { get; } = environmentId;
}
