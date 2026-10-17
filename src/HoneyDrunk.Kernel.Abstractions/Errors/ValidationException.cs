using HoneyDrunk.Kernel.Abstractions.Identity;
using System.Diagnostics.CodeAnalysis;

namespace HoneyDrunk.Kernel.Abstractions.Errors;

/// <summary>
/// Exception representing validation failures (input, invariant, contract).
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ValidationException"/> class.
/// </remarks>
/// <param name="message">Validation failure message.</param>
/// <param name="correlationId">Optional correlation identifier.</param>
/// <param name="errorCode">Optional structured error code (defaults to validation.input).</param>
/// <param name="nodeId">Optional originating Node identifier.</param>
/// <param name="environmentId">Optional environment identifier.</param>
/// <param name="innerException">Optional inner exception.</param>
[Serializable]
[SuppressMessage(
    "Major Code Smell",
    "S3925:\"ISerializable\" should be implemented correctly",
    Justification = "Legacy ISerializable pattern is obsolete in .NET 8+ (SYSLIB0051) and BinaryFormatter is removed (SYSLIB0011). [Serializable] retained for ABI compatibility; legacy deserialization ctor intentionally omitted.")]
public sealed class ValidationException(
    string message,
    CorrelationId? correlationId = null,
    ErrorCode? errorCode = null,
    NodeId? nodeId = null,
    EnvironmentId? environmentId = null,
    Exception? innerException = null) : HoneyDrunkException(
        message,
        correlationId,
        errorCode ?? new ErrorCode("validation.input"),
        nodeId,
        environmentId,
        innerException)
{
}
