using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Kernel.Abstractions.Errors;

/// <summary>
/// Exception indicating a failure in a downstream dependency (service, data store, external API).
/// Suggests transient or systemic availability issues.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DependencyFailureException"/> class.
/// </remarks>
/// <param name="message">Human-readable failure description.</param>
/// <param name="correlationId">Optional correlation identifier.</param>
/// <param name="errorCode">Optional structured error code (defaults to dependency.failure).</param>
/// <param name="nodeId">Optional originating Node identifier.</param>
/// <param name="environmentId">Optional environment identifier.</param>
/// <param name="innerException">Optional inner exception.</param>
[Serializable]
public sealed class DependencyFailureException(
    string message,
    CorrelationId? correlationId = null,
    ErrorCode? errorCode = null,
    NodeId? nodeId = null,
    EnvironmentId? environmentId = null,
    Exception? innerException = null) : HoneyDrunkException(
        message,
        correlationId,
        errorCode ?? new ErrorCode("dependency.failure"),
        nodeId,
        environmentId,
        innerException)
{
}
