using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Errors;
using HoneyDrunk.Kernel.Abstractions.Hosting;
using HoneyDrunk.Kernel.Abstractions.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Kernel.DependencyInjection;

/// <summary>
/// Validates that required HoneyDrunk services are registered.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated via GetService<IServiceProviderValidation>")]
internal sealed class ServiceProviderValidation : IServiceProviderValidation
{
    /// <inheritdoc />
    public void Validate(IServiceProvider services)
    {
        var logger = services.GetService<ILogger<ServiceProviderValidation>>();
        var errors = new List<string>();
        var warnings = new List<string>();

        // Validate core context services (required)
        ValidateRequired<INodeContext>(services, errors, "INodeContext", "AddHoneyDrunkNode()");
        ValidateRequired<IGridContextAccessor>(services, errors, "IGridContextAccessor", "AddHoneyDrunkNode()");
        ValidateRequired<IOperationContextAccessor>(services, errors, "IOperationContextAccessor", "AddHoneyDrunkNode()");
        ValidateRequired<IOperationContextFactory>(services, errors, "IOperationContextFactory", "AddHoneyDrunkNode()");

        // Validate hosting services (required)
        ValidateRequired<INodeDescriptor>(services, errors, "INodeDescriptor", "AddHoneyDrunkNode()");

        // Validate error handling (required)
        ValidateRequired<IErrorClassifier>(services, errors, "IErrorClassifier", "AddHoneyDrunkNode()");

        // Validate transport binders (recommended)
        ValidateRecommended<IEnumerable<ITransportEnvelopeBinder>>(services, warnings, "ITransportEnvelopeBinder", "AddHoneyDrunkNode() registers default binders");

        // Validate configuration (recommended)
        ValidateRecommended<IStudioConfiguration>(services, warnings, "IStudioConfiguration", "Configure studio settings for multi-environment support");

        // Log warnings
        if (warnings.Count > 0 && logger is not null)
        {
            foreach (var warning in warnings)
            {
                logger.LogWarning("Service validation warning: {Warning}", warning);
            }
        }

        // Throw if any required services are missing
        if (errors.Count > 0)
        {
            var errorMessage = string.Join(Environment.NewLine, errors);
            throw new InvalidOperationException(
                $"Required HoneyDrunk services are not registered:{Environment.NewLine}{errorMessage}");
        }

        logger?.LogInformation("Service validation completed successfully. All required services are registered.");
    }

    private static void ValidateRequired<T>(
        IServiceProvider services,
        List<string> errors,
        string serviceName,
        string registrationHint)
    {
        if (services.GetService<T>() is null)
        {
            errors.Add($"  - {serviceName} is missing. Register via: {registrationHint}");
        }
    }

    private static void ValidateRecommended<T>(
        IServiceProvider services,
        List<string> warnings,
        string serviceName,
        string registrationHint)
    {
        if (services.GetService<T>() is null)
        {
            warnings.Add($"{serviceName} is not registered. {registrationHint}");
        }
    }
}
