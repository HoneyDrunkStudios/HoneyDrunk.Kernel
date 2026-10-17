namespace HoneyDrunk.Kernel.Abstractions.Hosting;

/// <summary>
/// Represents Studio-level configuration that applies to all Nodes within a Studio.
/// </summary>
/// <remarks>
/// Studio configuration provides environment-wide settings that are shared across
/// all Nodes in a Studio (e.g., "honeycomb" production Studio, "staging" Studio).
/// This includes:
/// - Shared infrastructure endpoints (databases, message queues, caches).
/// - Authentication/authorization settings.
/// - Observability backend configurations.
/// - Feature flags and operational settings.
/// </remarks>
public interface IStudioConfiguration
{
    /// <summary>
    /// Gets the Studio identifier.
    /// Example: "honeycomb", "staging", "dev-alice".
    /// </summary>
    string StudioId { get; }

    /// <summary>
    /// Gets the environment name.
    /// Example: "production", "staging", "development".
    /// </summary>
    string Environment { get; }

    /// <summary>
    /// Gets the Vault endpoint URL for secrets management.
    /// </summary>
    string? VaultEndpoint { get; }

    /// <summary>
    /// Gets the observability backend configuration (e.g., OpenTelemetry collector endpoint).
    /// </summary>
    string? ObservabilityEndpoint { get; }

    /// <summary>
    /// Gets the service discovery endpoint (if using external service registry).
    /// </summary>
    string? ServiceDiscoveryEndpoint { get; }

    /// <summary>
    /// Gets Studio-wide feature flags.
    /// </summary>
    IReadOnlyDictionary<string, bool> FeatureFlags { get; }

    /// <summary>
    /// Gets Studio-wide tags/labels.
    /// </summary>
    IReadOnlyDictionary<string, string> Tags { get; }

    /// <summary>
    /// Attempts to get a configuration value by key.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The configuration value if found; otherwise null.</param>
    /// <returns>True if the value was found; otherwise false.</returns>
    bool TryGetValue(string key, out string? value);
}
