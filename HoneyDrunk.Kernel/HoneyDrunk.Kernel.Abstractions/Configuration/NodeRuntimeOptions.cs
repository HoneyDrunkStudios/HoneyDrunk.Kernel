namespace HoneyDrunk.Kernel.Abstractions.Configuration;

/// <summary>
/// Standard Node runtime configuration options shared across all Nodes.
/// </summary>
/// <remarks>
/// NodeRuntimeOptions define common operational settings that every Node needs.
/// These are typically loaded from configuration files or environment variables.
/// </remarks>
public sealed record NodeRuntimeOptions
{
    /// <summary>
    /// Gets or sets the environment name.
    /// Examples: "production", "staging", "development".
    /// </summary>
    public string Environment { get; set; } = "production";

    /// <summary>
    /// Gets or sets the deployment region.
    /// Examples: "us-east-1", "eu-west-1", "ap-southeast-2".
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets the deployment ring for gradual rollouts.
    /// Examples: "canary", "ring-1", "ring-2", "prod".
    /// </summary>
    public string? DeploymentRing { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether detailed telemetry is enabled.
    /// </summary>
    public bool EnableDetailedTelemetry { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether distributed tracing is enabled.
    /// </summary>
    public bool EnableDistributedTracing { get; set; } = true;

    /// <summary>
    /// Gets or sets the telemetry sampling rate (0.0 to 1.0).
    /// </summary>
    public double TelemetrySamplingRate { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the health check interval in seconds.
    /// </summary>
    public int HealthCheckIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the grace period for shutdown in seconds.
    /// </summary>
    public int ShutdownGracePeriodSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets a value indicating whether to enable automatic secret rotation.
    /// </summary>
    public bool EnableSecretRotation { get; set; } = true;

    /// <summary>
    /// Gets or sets the secret rotation check interval in minutes.
    /// </summary>
    public int SecretRotationIntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Gets the additional runtime tags.
    /// </summary>
    public Dictionary<string, string> Tags { get; init; } = [];
}
