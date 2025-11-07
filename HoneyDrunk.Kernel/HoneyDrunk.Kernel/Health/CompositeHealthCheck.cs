using HoneyDrunk.Kernel.Abstractions.Health;

namespace HoneyDrunk.Kernel.Health;

/// <summary>
/// Composite health check that aggregates multiple health checks and returns the worst status.
/// </summary>
/// <param name="checks">The collection of health checks to aggregate.</param>
public sealed class CompositeHealthCheck(IEnumerable<IHealthCheck> checks) : IHealthCheck
{
    private readonly IHealthCheck[] _checks = [.. checks];

    /// <inheritdoc />
    public async Task<HealthStatus> CheckAsync(CancellationToken cancellationToken = default)
    {
        if (_checks.Length == 0)
        {
            return HealthStatus.Healthy;
        }

        var results = await Task.WhenAll(_checks.Select(check => check.CheckAsync(cancellationToken)));

        if (results.Any(status => status == HealthStatus.Unhealthy))
        {
            return HealthStatus.Unhealthy;
        }

        if (results.Any(status => status == HealthStatus.Degraded))
        {
            return HealthStatus.Degraded;
        }

        return HealthStatus.Healthy;
    }
}
