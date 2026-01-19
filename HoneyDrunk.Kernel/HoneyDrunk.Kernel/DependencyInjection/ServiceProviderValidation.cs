using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Errors;
using HoneyDrunk.Kernel.Abstractions.Hosting;
using HoneyDrunk.Kernel.Abstractions.Lifecycle;
using HoneyDrunk.Kernel.Abstractions.Transport;
using HoneyDrunk.Kernel.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

        // Create a scope to validate scoped services (avoids "Cannot resolve scoped service from root provider")
        using var scope = services.CreateScope();
        var scopedServices = scope.ServiceProvider;

        // Validate core context services (required)
        // Note: Use scopedServices for scoped registrations, services for singletons
        ValidateRequired<INodeContext>(services, errors, "INodeContext", "AddHoneyDrunkNode()");
        ValidateRequired<IGridContextAccessor>(services, errors, "IGridContextAccessor", "AddHoneyDrunkNode()");
        ValidateRequired<IOperationContextAccessor>(services, errors, "IOperationContextAccessor", "AddHoneyDrunkNode()");
        ValidateRequiredScoped<IOperationContextFactory>(scopedServices, errors, "IOperationContextFactory", "AddHoneyDrunkNode()");

        // Validate hosting services (required)
        ValidateRequired<INodeDescriptor>(services, errors, "INodeDescriptor", "AddHoneyDrunkNode()");

        // Validate error handling (required)
        ValidateRequired<IErrorClassifier>(services, errors, "IErrorClassifier", "AddHoneyDrunkNode()");

        // Validate lifecycle coordination (required in v3)
        ValidateRequired<NodeLifecycleManager>(services, errors, "NodeLifecycleManager", "AddHoneyDrunkNode()");
        ValidateLifecycleHost(services, errors);

        // Validate transport binders (recommended)
        ValidateRecommended<IEnumerable<ITransportEnvelopeBinder>>(services, warnings, "ITransportEnvelopeBinder", "AddHoneyDrunkNode() registers default binders");

        // Validate configuration (recommended)
        ValidateRecommended<IStudioConfiguration>(services, warnings, "IStudioConfiguration", "Configure studio settings for multi-environment support");

        // Validate optional but recommended services
        ValidateOptional<IStartupHook>(services, warnings, "IStartupHook", "Register startup hooks for Node initialization logic");
        ValidateOptional<IShutdownHook>(services, warnings, "IShutdownHook", "Register shutdown hooks for graceful cleanup");
        ValidateOptional<IHealthContributor>(services, warnings, "IHealthContributor", "Register health contributors for /health endpoint monitoring");
        ValidateOptional<IReadinessContributor>(services, warnings, "IReadinessContributor", "Register readiness contributors for /ready endpoint traffic gating");

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

    private static void ValidateRequiredScoped<T>(
        IServiceProvider scopedServices,
        List<string> errors,
        string serviceName,
        string registrationHint)
    {
        // scopedServices should be from a created scope to avoid "Cannot resolve scoped service from root provider"
        if (scopedServices.GetService<T>() is null)
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

    private static void ValidateOptional<T>(
        IServiceProvider services,
        List<string> warnings,
        string serviceName,
        string registrationHint)
    {
        var hasService = services.GetServices<T>().Any();
        if (!hasService)
        {
            warnings.Add($"{serviceName} implementations not found. {registrationHint}");
        }
    }

    private static void ValidateLifecycleHost(
        IServiceProvider services,
        List<string> errors)
    {
        var hostedServices = services.GetServices<IHostedService>();
        var hasLifecycleHost = hostedServices.Any(h => h.GetType().Name == "NodeLifecycleHost");

        if (!hasLifecycleHost)
        {
            errors.Add("  - NodeLifecycleHost (IHostedService) is missing. Register via: AddHoneyDrunkNode()");
        }
    }
}
