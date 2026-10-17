namespace HoneyDrunk.Kernel.Abstractions.Health;

/// <summary>
/// Provides a mechanism for checking the health of a component or service.
/// </summary>
public interface IHealthCheck
{
    /// <summary>
    /// Performs a health check asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The health status of the component.</returns>
    Task<HealthStatus> CheckAsync(CancellationToken cancellationToken = default);
}
