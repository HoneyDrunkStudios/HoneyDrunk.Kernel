using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Kernel.Abstractions.Errors;

/// <summary>
/// Exception for missing resources (entity, document, capability, etc.).
/// Maps to a Not Found classification.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="NotFoundException"/> class.
/// </remarks>
/// <param name="message">Human-readable description of the missing resource.</param>
/// <param name="correlationId">Optional correlation identifier.</param>
/// <param name="errorCode">Optional structured error code (defaults to resource.notfound).</param>
/// <param name="nodeId">Optional originating Node identifier.</param>
/// <param name="environmentId">Optional environment identifier.</param>
/// <param name="innerException">Optional inner exception.</param>
[Serializable]
public sealed class NotFoundException(
    string message,
    CorrelationId? correlationId = null,
    ErrorCode? errorCode = null,
    NodeId? nodeId = null,
    EnvironmentId? environmentId = null,
    Exception? innerException = null) : HoneyDrunkException(
        message,
        correlationId,
        errorCode ?? new ErrorCode("resource.notfound"),
        nodeId,
        environmentId,
        innerException)
{
}
