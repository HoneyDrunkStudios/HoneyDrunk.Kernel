using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Kernel.Abstractions.Errors;

/// <summary>
/// Exception representing an optimistic concurrency conflict (competing updates / CAS violation).
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ConcurrencyException"/> class.
/// </remarks>
/// <param name="message">Human-readable conflict description.</param>
/// <param name="correlationId">Optional correlation identifier.</param>
/// <param name="errorCode">Optional structured error code (defaults to concurrency.conflict).</param>
/// <param name="nodeId">Optional originating Node identifier.</param>
/// <param name="environmentId">Optional environment identifier.</param>
/// <param name="innerException">Optional inner exception.</param>
[Serializable]
public sealed class ConcurrencyException(
    string message,
    CorrelationId? correlationId = null,
    ErrorCode? errorCode = null,
    NodeId? nodeId = null,
    EnvironmentId? environmentId = null,
    Exception? innerException = null) : HoneyDrunkException(
        message,
        correlationId,
        errorCode ?? new ErrorCode("concurrency.conflict"),
        nodeId,
        environmentId,
        innerException)
{
}
