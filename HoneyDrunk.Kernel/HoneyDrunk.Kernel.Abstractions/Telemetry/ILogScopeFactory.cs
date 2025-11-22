namespace HoneyDrunk.Kernel.Abstractions.Telemetry;

/// <summary>
/// Creates logging scopes enriched with Grid context.
/// </summary>
/// <remarks>
/// ILogScopeFactory provides a way to create structured logging scopes that automatically
/// include Grid-wide context (correlation IDs, Node identity, etc.) without requiring
/// loggers to manually add these fields to every log statement.
/// </remarks>
public interface ILogScopeFactory
{
    /// <summary>
    /// Creates a logging scope with telemetry context.
    /// </summary>
    /// <param name="context">The telemetry context to include in the scope.</param>
    /// <returns>A disposable scope that ends when disposed.</returns>
    IDisposable CreateScope(ITelemetryContext context);

    /// <summary>
    /// Creates a logging scope with custom properties in addition to telemetry context.
    /// </summary>
    /// <param name="context">The telemetry context to include in the scope.</param>
    /// <param name="additionalProperties">Additional properties to include in the scope.</param>
    /// <returns>A disposable scope that ends when disposed.</returns>
    IDisposable CreateScope(ITelemetryContext context, IReadOnlyDictionary<string, object?> additionalProperties);
}
