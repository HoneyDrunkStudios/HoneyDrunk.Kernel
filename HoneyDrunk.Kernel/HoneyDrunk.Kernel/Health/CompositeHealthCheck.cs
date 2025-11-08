// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using HoneyDrunk.Kernel.Abstractions.Health;

namespace HoneyDrunk.Kernel.Health;

/// <summary>
/// Composite health check that aggregates multiple health checks and returns the worst status.
/// Any health check that throws an exception is treated as Unhealthy.
/// </summary>
/// <param name="checks">The collection of health checks to aggregate.</param>
public sealed class CompositeHealthCheck(IEnumerable<IHealthCheck> checks) : IHealthCheck
{
    private readonly IHealthCheck[] checks = [.. checks];

    /// <inheritdoc />
    public async Task<HealthStatus> CheckAsync(CancellationToken cancellationToken = default)
    {
        if (this.checks.Length == 0)
        {
            return HealthStatus.Healthy;
        }

        var checkTasks = this.checks.Select(check => ExecuteCheckSafelyAsync(check, cancellationToken));
        var results = await Task.WhenAll(checkTasks);

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

    private static async Task<HealthStatus> ExecuteCheckSafelyAsync(IHealthCheck check, CancellationToken cancellationToken)
    {
        try
        {
            return await check.CheckAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return HealthStatus.Unhealthy;
        }
    }
}
