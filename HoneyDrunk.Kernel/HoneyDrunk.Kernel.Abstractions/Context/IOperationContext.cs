namespace HoneyDrunk.Kernel.Abstractions.Context;

/// <summary>
/// Represents a bounded operation within the Grid, tracking timing, outcome, and telemetry.
/// </summary>
/// <remarks>
/// OperationContext wraps a unit of work (e.g., HTTP request, message processing, background job)
/// and provides standardized telemetry, timing, and outcome tracking. It bridges the gap between
/// the long-lived NodeContext and the flowing GridContext.
/// </remarks>
public interface IOperationContext : IDisposable
{
    /// <summary>
    /// Gets the Grid context for this operation.
    /// </summary>
    IGridContext GridContext { get; }

    /// <summary>
    /// Gets the name/type of this operation.
    /// Example: "ProcessPayment", "HandleWebhook", "SyncInventory".
    /// </summary>
    string OperationName { get; }

    /// <summary>
    /// Gets the UTC timestamp when this operation started.
    /// </summary>
    DateTimeOffset StartedAtUtc { get; }

    /// <summary>
    /// Gets the UTC timestamp when this operation completed, or null if still running.
    /// </summary>
    DateTimeOffset? CompletedAtUtc { get; }

    /// <summary>
    /// Gets whether this operation completed successfully.
    /// </summary>
    bool? IsSuccess { get; }

    /// <summary>
    /// Gets the error message if the operation failed, or null if successful.
    /// </summary>
    string? ErrorMessage { get; }

    /// <summary>
    /// Gets optional metadata associated with this operation.
    /// Example: request path, message type, job parameters.
    /// </summary>
    IReadOnlyDictionary<string, object?> Metadata { get; }

    /// <summary>
    /// Marks this operation as completed successfully.
    /// </summary>
    void Complete();

    /// <summary>
    /// Marks this operation as failed with an error.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <param name="exception">Optional exception that caused the failure.</param>
    void Fail(string errorMessage, Exception? exception = null);

    /// <summary>
    /// Adds metadata to this operation context.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    void AddMetadata(string key, object? value);
}
