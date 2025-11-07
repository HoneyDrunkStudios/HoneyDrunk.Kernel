namespace HoneyDrunk.Kernel.Abstractions.Context;

/// <summary>
/// Provides contextual information for the current operation including correlation, causation, and baggage.
/// </summary>
public interface IKernelContext
{
    /// <summary>
    /// Gets the correlation identifier that groups related operations.
    /// </summary>
    string CorrelationId { get; }

    /// <summary>
    /// Gets the causation identifier indicating which operation triggered this one.
    /// </summary>
    string? CausationId { get; }

    /// <summary>
    /// Gets the cancellation token for the current operation.
    /// </summary>
    CancellationToken Cancellation { get; }

    /// <summary>
    /// Gets the baggage (key-value pairs) propagated with this context.
    /// </summary>
    IReadOnlyDictionary<string, string> Baggage { get; }

    /// <summary>
    /// Begins a new logical scope for the context.
    /// </summary>
    /// <returns>A disposable object that ends the scope when disposed.</returns>
    IDisposable BeginScope();
}
