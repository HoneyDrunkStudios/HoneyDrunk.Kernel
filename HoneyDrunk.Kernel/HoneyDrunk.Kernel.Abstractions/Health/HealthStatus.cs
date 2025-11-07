namespace HoneyDrunk.Kernel.Abstractions.Health;

/// <summary>
/// Represents the health status of a component or system.
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// The component is healthy and functioning normally.
    /// </summary>
    Healthy,

    /// <summary>
    /// The component is functioning but with reduced capability or performance.
    /// </summary>
    Degraded,

    /// <summary>
    /// The component is unhealthy and not functioning correctly.
    /// </summary>
    Unhealthy
}
