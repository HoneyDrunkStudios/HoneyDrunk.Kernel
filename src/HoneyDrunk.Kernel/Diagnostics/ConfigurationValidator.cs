using HoneyDrunk.Kernel.Abstractions.Hosting;
using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Kernel.Diagnostics;

/// <summary>
/// Validates configuration at startup to catch issues early.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ConfigurationValidator"/> class.
/// </remarks>
/// <param name="logger">The logger instance.</param>
public sealed class ConfigurationValidator(ILogger<ConfigurationValidator> logger)
{
    private readonly ILogger<ConfigurationValidator> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Validates Node descriptor configuration.
    /// </summary>
    /// <param name="descriptor">The Node descriptor to validate.</param>
    /// <param name="errors">Output list of validation errors.</param>
    /// <returns>True if validation passed; otherwise false.</returns>
    public bool ValidateNodeDescriptor(INodeDescriptor descriptor, out List<string> errors)
    {
        errors = [];

        if (string.IsNullOrWhiteSpace(descriptor.NodeId))
        {
            errors.Add("NodeId is required");
        }

        if (string.IsNullOrWhiteSpace(descriptor.Version))
        {
            errors.Add("Version is required");
        }

        if (string.IsNullOrWhiteSpace(descriptor.Name))
        {
            errors.Add("Name is required");
        }

        // Validate capabilities: one error per empty name, one per duplicate name.
        var emptyCapabilityCount = descriptor.Capabilities.Count(c => string.IsNullOrWhiteSpace(c.Name));
        for (var i = 0; i < emptyCapabilityCount; i++)
        {
            errors.Add("Capability name cannot be empty");
        }

        var duplicateCapabilityNames = descriptor.Capabilities
            .Select(c => c.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .GroupBy(name => name)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);
        foreach (var duplicate in duplicateCapabilityNames)
        {
            errors.Add($"Duplicate capability name: {duplicate}");
        }

        // Validate dependencies
        var emptyDependencyCount = descriptor.Dependencies.Count(static dependency => string.IsNullOrWhiteSpace(dependency));
        for (var i = 0; i < emptyDependencyCount; i++)
        {
            errors.Add("Dependency cannot be empty");
        }

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                _logger.LogError("Configuration validation error: {Error}", error);
            }

            return false;
        }

        _logger.LogInformation("Node descriptor validation passed for {NodeId}", descriptor.NodeId);
        return true;
    }

    /// <summary>
    /// Validates Studio configuration.
    /// </summary>
    /// <param name="config">The Studio configuration to validate.</param>
    /// <param name="warnings">Output list of validation warnings.</param>
    /// <returns>True if validation passed; otherwise false.</returns>
    public bool ValidateStudioConfiguration(IStudioConfiguration config, out List<string> warnings)
    {
        warnings = [];

        if (string.IsNullOrWhiteSpace(config.VaultEndpoint))
        {
            warnings.Add("VaultEndpoint is not configured - secrets management may be limited");
        }

        if (string.IsNullOrWhiteSpace(config.ObservabilityEndpoint))
        {
            warnings.Add("ObservabilityEndpoint is not configured - telemetry may be degraded");
        }

        foreach (var warning in warnings)
        {
            _logger.LogWarning("Configuration warning: {Warning}", warning);
        }

        _logger.LogInformation("Studio configuration validated for {StudioId}", config.StudioId);
        return true;
    }
}
