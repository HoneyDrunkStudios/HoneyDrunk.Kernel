using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Kernel.Abstractions.Errors;

/// <summary>
/// Exception representing authorization/authentication related failures.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SecurityException"/> class.
/// </remarks>
/// <param name="message">Human-readable failure description.</param>
/// <param name="correlationId">Optional correlation identifier.</param>
/// <param name="errorCode">Optional structured error code (defaults to security.access.denied).</param>
/// <param name="nodeId">Optional originating Node identifier.</param>
/// <param name="environmentId">Optional environment identifier.</param>
/// <param name="innerException">Optional inner exception.</param>
[Serializable]
public sealed class SecurityException(
    string message,
    CorrelationId? correlationId = null,
    ErrorCode? errorCode = null,
    NodeId? nodeId = null,
    EnvironmentId? environmentId = null,
    Exception? innerException = null) : HoneyDrunkException(
        message,
        correlationId,
        errorCode ?? new ErrorCode("security.access.denied"),
        nodeId,
        environmentId,
        innerException)
{
}
