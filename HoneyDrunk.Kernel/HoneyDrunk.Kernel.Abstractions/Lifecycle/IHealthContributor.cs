using HoneyDrunk.Kernel.Abstractions.Health;

namespace HoneyDrunk.Kernel.Abstractions.Lifecycle;

/// <summary>
/// Contributes to the overall health status of a Node.
/// </summary>
/// <remarks>
/// Health contributors check if components are functioning correctly.
/// Unlike IHealthCheck (which is standalone), IHealthContributor participates
/// in a coordinated health assessment managed by the Node lifecycle.
/// Multiple contributors are aggregated to determine overall Node health.
/// </remarks>
public interface IHealthContributor
{
    /// <summary>
    /// Gets the name of this health contributor.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the priority/order of this contributor (lower runs first).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Gets a value indicating whether this contributor is critical.
    /// If true, unhealthy status will immediately mark the Node as unhealthy.
    /// </summary>
    bool IsCritical { get; }

    /// <summary>
    /// Checks the health of this component.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The health status and optional diagnostic message.</returns>
    Task<(HealthStatus status, string? message)> CheckHealthAsync(CancellationToken cancellationToken = default);
}
