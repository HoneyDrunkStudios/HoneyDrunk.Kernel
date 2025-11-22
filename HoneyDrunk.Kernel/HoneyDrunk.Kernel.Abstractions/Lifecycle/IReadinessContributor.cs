namespace HoneyDrunk.Kernel.Abstractions.Lifecycle;

/// <summary>
/// Determines if a Node is ready to accept traffic or work.
/// </summary>
/// <remarks>
/// Readiness is different from health:
/// - Health: Is the Node functioning correctly?
/// - Readiness: Is the Node ready to handle requests?
///
/// A Node can be healthy but not ready (e.g., still warming up caches).
/// Readiness checks gate traffic routing and load balancer inclusion.
/// Examples: database connections established, caches warmed, configuration loaded.
/// </remarks>
public interface IReadinessContributor
{
    /// <summary>
    /// Gets the name of this readiness contributor.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the priority/order of this contributor (lower runs first).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Gets a value indicating whether this contributor is required for readiness.
    /// If true, this must be ready for the Node to be considered ready.
    /// </summary>
    bool IsRequired { get; }

    /// <summary>
    /// Checks if this component is ready.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if ready; otherwise false with optional reason.</returns>
    Task<(bool isReady, string? reason)> CheckReadinessAsync(CancellationToken cancellationToken = default);
}
