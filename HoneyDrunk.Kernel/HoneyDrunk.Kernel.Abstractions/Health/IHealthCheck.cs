// <copyright file="IHealthCheck.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

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
