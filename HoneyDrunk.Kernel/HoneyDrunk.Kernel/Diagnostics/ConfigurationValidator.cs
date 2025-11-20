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

        // Validate capabilities
        var capabilityNames = new HashSet<string>();
        foreach (var capability in descriptor.Capabilities)
        {
            if (string.IsNullOrWhiteSpace(capability.Name))
            {
                errors.Add("Capability name cannot be empty");
            }
            else if (!capabilityNames.Add(capability.Name))
            {
                errors.Add($"Duplicate capability name: {capability.Name}");
            }
        }

        // Validate dependencies
        if (descriptor.Dependencies != null)
        {
            foreach (var dependency in descriptor.Dependencies)
            {
                if (string.IsNullOrWhiteSpace(dependency))
                {
                    errors.Add("Dependency cannot be empty");
                }
            }
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
