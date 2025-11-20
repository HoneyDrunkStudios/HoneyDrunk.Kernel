using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Diagnostics;
using HoneyDrunk.Kernel.Abstractions.Hosting;
using HoneyDrunk.Kernel.Abstractions.Lifecycle;
using HoneyDrunk.Kernel.Context;
using HoneyDrunk.Kernel.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Kernel.Hosting;

/// <summary>
/// Extension methods for registering HoneyDrunk Grid services.
/// </summary>
public static class HoneyDrunkServiceCollectionExtensions
{
    /// <summary>
    /// Registers HoneyDrunk Grid services with Node identity and context management.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for Grid options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHoneyDrunkGrid(
        this IServiceCollection services,
        Action<GridOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new GridOptions();
        configure(options);

        if (string.IsNullOrWhiteSpace(options.NodeId))
        {
            throw new InvalidOperationException("NodeId must be configured when adding HoneyDrunk Grid services.");
        }

        if (string.IsNullOrWhiteSpace(options.StudioId))
        {
            throw new InvalidOperationException("StudioId must be configured when adding HoneyDrunk Grid services.");
        }

        services.AddSingleton<INodeContext>(sp =>
            new NodeContext(
                options.NodeId,
                options.Version,
                options.StudioId,
                options.Environment,
                options.Tags));

        services.AddSingleton<IMetricsCollector, NoOpMetricsCollector>();

        services.AddScoped<IGridContext>(sp =>
        {
            var nodeContext = sp.GetRequiredService<INodeContext>();
            return new GridContext(
                correlationId: Ulid.NewUlid().ToString(),
                nodeId: nodeContext.NodeId,
                studioId: nodeContext.StudioId,
                environment: nodeContext.Environment);
        });

        services.AddHostedService<NodeLifecycleHost>();

        return services;
    }

    /// <summary>
    /// Registers a Node lifecycle implementation.
    /// </summary>
    /// <typeparam name="TLifecycle">The lifecycle implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNodeLifecycle<TLifecycle>(this IServiceCollection services)
        where TLifecycle : class, INodeLifecycle
    {
        services.AddSingleton<INodeLifecycle, TLifecycle>();
        return services;
    }

    /// <summary>
    /// Registers a startup hook.
    /// </summary>
    /// <typeparam name="THook">The startup hook type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddStartupHook<THook>(this IServiceCollection services)
        where THook : class, IStartupHook
    {
        services.AddSingleton<IStartupHook, THook>();
        return services;
    }

    /// <summary>
    /// Registers a shutdown hook.
    /// </summary>
    /// <typeparam name="THook">The shutdown hook type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddShutdownHook<THook>(this IServiceCollection services)
        where THook : class, IShutdownHook
    {
        services.AddSingleton<IShutdownHook, THook>();
        return services;
    }
}
