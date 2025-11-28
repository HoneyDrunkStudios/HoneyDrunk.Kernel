using HoneyDrunk.Kernel.Abstractions.Context;
using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Kernel.Context;

/// <summary>
/// Default implementation of IOperationContext for operation boundary tracking.
/// </summary>
public sealed class OperationContext : IOperationContext
{
    private readonly ILogger? _logger;
    private readonly Dictionary<string, object?> _metadata;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationContext"/> class.
    /// </summary>
    /// <param name="gridContext">The Grid context for this operation.</param>
    /// <param name="operationName">The name/type of this operation.</param>
    /// <param name="operationId">The operation identifier (span-id) for this operation.</param>
    /// <param name="logger">Optional logger for operation telemetry.</param>
    /// <param name="metadata">Optional initial metadata.</param>
    public OperationContext(
        IGridContext gridContext,
        string operationName,
        string operationId,
        ILogger? logger = null,
        IReadOnlyDictionary<string, object?>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(gridContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);

        GridContext = gridContext;
        OperationName = operationName;
        OperationId = operationId;
        _logger = logger;
        StartedAtUtc = DateTimeOffset.UtcNow;
        _metadata = metadata != null
            ? new Dictionary<string, object?>(metadata)
            : [];

        _logger?.LogInformation(
            "Operation {OperationName} started with CorrelationId {CorrelationId} and OperationId {OperationId} on Node {NodeId}",
            OperationName,
            GridContext.CorrelationId,
            OperationId,
            GridContext.NodeId);
    }

    /// <inheritdoc />
    public IGridContext GridContext { get; }

    /// <inheritdoc />
    public string OperationName { get; }

    /// <inheritdoc />
    public string OperationId { get; }

    /// <inheritdoc />
    public string CorrelationId => GridContext.CorrelationId;

    /// <inheritdoc />
    public string? CausationId => GridContext.CausationId;

    /// <inheritdoc />
    public string? TenantId => GridContext.TenantId;

    /// <inheritdoc />
    public string? ProjectId => GridContext.ProjectId;

    /// <inheritdoc />
    public DateTimeOffset StartedAtUtc { get; }

    /// <inheritdoc />
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    /// <inheritdoc />
    public bool? IsSuccess { get; private set; }

    /// <inheritdoc />
    public string? ErrorMessage { get; private set; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Metadata => _metadata;

    /// <inheritdoc />
    public void Complete()
    {
        if (IsSuccess.HasValue)
        {
            return;
        }

        IsSuccess = true;
        CompletedAtUtc = DateTimeOffset.UtcNow;

        var duration = CompletedAtUtc.Value - StartedAtUtc;
        _logger?.LogInformation(
            "Operation {OperationName} completed successfully in {DurationMs}ms (CorrelationId: {CorrelationId}, OperationId: {OperationId})",
            OperationName,
            duration.TotalMilliseconds,
            GridContext.CorrelationId,
            OperationId);
    }

    /// <inheritdoc />
    public void Fail(string errorMessage, Exception? exception = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        if (IsSuccess.HasValue)
        {
            return;
        }

        IsSuccess = false;
        CompletedAtUtc = DateTimeOffset.UtcNow;
        ErrorMessage = errorMessage;

        var duration = CompletedAtUtc.Value - StartedAtUtc;
        _logger?.LogError(
            exception,
            "Operation {OperationName} failed after {DurationMs}ms: {ErrorMessage} (CorrelationId: {CorrelationId}, OperationId: {OperationId})",
            OperationName,
            duration.TotalMilliseconds,
            errorMessage,
            GridContext.CorrelationId,
            OperationId);
    }

    /// <inheritdoc />
    public void AddMetadata(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _metadata[key] = value;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (!IsSuccess.HasValue)
        {
            Complete();
        }

        _disposed = true;
    }
}
