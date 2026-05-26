using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Hosting;
using HoneyDrunk.Kernel.Context;
using HoneyDrunk.Kernel.Lifecycle;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Kernel.DependencyInjection;

/// <summary>
/// Extension methods for registering core HoneyDrunk Node services with guardrails.
/// </summary>
public static class HoneyDrunkCoreExtensions
{
    /// <summary>
    /// Registers core HoneyDrunk Node services with validation and guardrails.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="nodeDescriptor">The Node descriptor.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHoneyDrunkCoreNode(
        this IServiceCollection services,
        INodeDescriptor nodeDescriptor)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ArgumentNullException.ThrowIfNull(nodeDescriptor, nameof(nodeDescriptor));

        // Validate Node descriptor
        ValidateNodeDescriptor(nodeDescriptor);

        // Register Node descriptor
        services.AddSingleton(nodeDescriptor);

        // Register NodeContext from descriptor
        services.AddSingleton<INodeContext>(sp =>
        {
            var tags = nodeDescriptor.Tags.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // Use descriptor properties directly, not manifest
            var studioId = nodeDescriptor.Manifest?.Tags.TryGetValue("studio-id", out var studio) == true ? studio : "default";
            var environment = nodeDescriptor.Manifest?.Tags.TryGetValue("environment", out var env) == true ? env : "development";

            return new NodeContext(
                nodeDescriptor.NodeId,
                nodeDescriptor.Version,
                studioId,
                environment,
                tags);
        });

        // Register Grid context accessor
        services.AddSingleton<IGridContextAccessor, GridContextAccessor>();

        // Register lifecycle manager
        services.AddSingleton<NodeLifecycleManager>();

        return services;
    }

    /// <summary>
    /// Validates that all required services are registered.
    /// </summary>
    /// <param name="services">The service provider to validate.</param>
    public static void ValidateHoneyDrunkServices(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));

        var validator = services.GetService<IServiceProviderValidation>();
        validator?.Validate(services);
    }

    private static void ValidateNodeDescriptor(INodeDescriptor descriptor)
    {
        if (string.IsNullOrWhiteSpace(descriptor.NodeId))
        {
            throw new InvalidOperationException("NodeDescriptor must have a valid NodeId");
        }

        if (string.IsNullOrWhiteSpace(descriptor.Version))
        {
            throw new InvalidOperationException("NodeDescriptor must have a valid Version");
        }
    }
}
