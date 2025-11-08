// <copyright file="HealthStatus.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace HoneyDrunk.Kernel.Abstractions.Health;

/// <summary>
/// Represents the health status of a component or service.
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// The component is healthy and functioning normally.
    /// </summary>
    Healthy,

    /// <summary>
    /// The component is degraded but still functional.
    /// </summary>
    Degraded,

    /// <summary>
    /// The component is unhealthy and not functioning.
    /// </summary>
    Unhealthy,
}
