namespace HoneyDrunk.Kernel.Abstractions.Health;

/// <summary>
/// Performs health checks on a component or system.
/// </summary>
public interface IHealthCheck
{
    /// <summary>
    /// Performs a health check asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>The health status of the component.</returns>
    Task<HealthStatus> CheckAsync(CancellationToken cancellationToken = default);
}
