using HoneyDrunk.Kernel.Abstractions.Configuration;
using HoneyDrunk.Kernel.Abstractions.Hosting;
using Microsoft.Extensions.Configuration;

namespace HoneyDrunk.Kernel.Configuration;

/// <summary>
/// Implementation of IStudioConfiguration backed by IConfiguration and ISecretsSource.
/// </summary>
public sealed class StudioConfiguration : IStudioConfiguration
{
    private readonly IConfiguration _configuration;
    private readonly ISecretsSource? _secretsSource;
    private readonly Dictionary<string, bool> _featureFlags;
    private readonly Dictionary<string, string> _tags;

    /// <summary>
    /// Initializes a new instance of the <see cref="StudioConfiguration"/> class.
    /// </summary>
    /// <param name="studioId">The Studio identifier.</param>
    /// <param name="environment">The environment name.</param>
    /// <param name="configuration">The configuration provider.</param>
    /// <param name="secretsSource">Optional secrets source for secure configuration.</param>
    public StudioConfiguration(
        string studioId,
        string environment,
        IConfiguration configuration,
        ISecretsSource? secretsSource = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(studioId, nameof(studioId));
        ArgumentException.ThrowIfNullOrWhiteSpace(environment, nameof(environment));
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        StudioId = studioId;
        Environment = environment;
        _configuration = configuration;
        _secretsSource = secretsSource;

        // Load configuration sections
        VaultEndpoint = _configuration["Studio:VaultEndpoint"];
        ObservabilityEndpoint = _configuration["Studio:ObservabilityEndpoint"];
        ServiceDiscoveryEndpoint = _configuration["Studio:ServiceDiscoveryEndpoint"];

        // Load feature flags
        _featureFlags = [];
        var featureFlagsSection = _configuration.GetSection("Studio:FeatureFlags");
        _featureFlags = featureFlagsSection
            .GetChildren()
            .Select(child => (key: child.Key, value: child.Value))
            .Where(item => bool.TryParse(item.value, out _))
            .ToDictionary(
                item => item.key,
                item => bool.Parse(item.value!));

        // Load tags
        _tags = [];
        var tagsSection = _configuration.GetSection("Studio:Tags");
        _tags = tagsSection
            .GetChildren()
            .Where(child => child.Value != null)
            .ToDictionary(
                child => child.Key,
                child => child.Value!);
    }

    /// <inheritdoc />
    public string StudioId { get; }

    /// <inheritdoc />
    public string Environment { get; }

    /// <inheritdoc />
    public string? VaultEndpoint { get; }

    /// <inheritdoc />
    public string? ObservabilityEndpoint { get; }

    /// <inheritdoc />
    public string? ServiceDiscoveryEndpoint { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, bool> FeatureFlags => _featureFlags;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Tags => _tags;

    /// <inheritdoc />
    public bool TryGetValue(string key, out string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));

        // Try configuration first
        value = _configuration[key];
        if (value != null)
        {
            return true;
        }

        // Try secrets source if available
        if (_secretsSource != null && _secretsSource.TryGetSecret(key, out value))
        {
            return true;
        }

        value = null;
        return false;
    }
}
