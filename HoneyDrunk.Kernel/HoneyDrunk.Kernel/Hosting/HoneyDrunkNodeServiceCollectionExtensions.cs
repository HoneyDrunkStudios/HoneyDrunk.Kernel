using HoneyDrunk.Kernel.Abstractions.Agents;
using HoneyDrunk.Kernel.Abstractions.Configuration;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Errors;
using HoneyDrunk.Kernel.Abstractions.Hosting;
using HoneyDrunk.Kernel.Abstractions.Lifecycle;
using HoneyDrunk.Kernel.Abstractions.Transport;
using HoneyDrunk.Kernel.AgentsInterop;
using HoneyDrunk.Kernel.Context;
using HoneyDrunk.Kernel.DependencyInjection;
using HoneyDrunk.Kernel.Errors;
using HoneyDrunk.Kernel.Lifecycle;
using HoneyDrunk.Kernel.Telemetry;
using HoneyDrunk.Kernel.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;

namespace HoneyDrunk.Kernel.Hosting;

/// <summary>
/// Bootstrap extensions for converting a plain application into a HoneyDrunk Node.
/// </summary>
public static class HoneyDrunkNodeServiceCollectionExtensions
{
    /// <summary>
    /// Adds HoneyDrunk Node runtime services (identity, contexts, descriptor, lifecycle, telemetry) and returns a fluent builder for further configuration.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configure">Delegate to configure <see cref="HoneyDrunkNodeOptions"/>.</param>
    /// <returns>An <see cref="IHoneyDrunkBuilder"/> for optional chained configuration.</returns>
    public static IHoneyDrunkBuilder AddHoneyDrunkNode(this IServiceCollection services, Action<HoneyDrunkNodeOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new HoneyDrunkNodeOptions();
        configure(options);
        options.Validate();

        // Resolve version fallback if not set.
        options.Version ??= typeof(HoneyDrunkNodeServiceCollectionExtensions).Assembly.GetName().Version?.ToString() ?? "1.0.0";

        // Register options (IOptions pattern friendly).
        services.AddSingleton(options);
        services.AddSingleton<IOptions<HoneyDrunkNodeOptions>>(sp => new OptionsWrapper<HoneyDrunkNodeOptions>(sp.GetRequiredService<HoneyDrunkNodeOptions>()));

        // NodeContext (process scoped identity) derived from Node options and grid config.
        services.AddSingleton<INodeContext>(sp =>
        {
            var opts = sp.GetRequiredService<HoneyDrunkNodeOptions>();
            var gridRoot = sp.GetService<IOptions<HoneyDrunkGridOptions>>()?.Value;
            var studio = !string.IsNullOrWhiteSpace(opts.StudioId) ? opts.StudioId : gridRoot?.StudioId ?? string.Empty;
            var environment = opts.EnvironmentId?.Value ?? gridRoot?.Environment ?? string.Empty;

            return new NodeContext(
                nodeId: opts.NodeId!.Value,
                version: opts.Version!,
                studioId: studio,
                environment: environment,
                tags: opts.Tags);
        });

        // NodeDescriptor registered via factory using NodeContext for identity.
        services.AddSingleton<INodeDescriptor>(sp =>
        {
            var opts = sp.GetRequiredService<HoneyDrunkNodeOptions>();
            var nc = sp.GetRequiredService<INodeContext>();
            var descriptor = new NodeDescriptor(
                nodeId: nc.NodeId,
                version: nc.Version,
                name: nc.NodeId,
                description: nc.NodeId,
                sector: opts.SectorId!.Value,
                studioId: nc.StudioId,
                environment: nc.Environment,
                tags: nc.Tags);
            return descriptor;
        });

        // Ambient accessors + factories.
        services.AddSingleton<IGridContextAccessor, GridContextAccessor>();
        services.AddSingleton<IOperationContextAccessor, OperationContextAccessor>();
        services.AddSingleton<IGridContextFactory, GridContextFactory>();
        services.AddScoped<IOperationContextFactory, OperationContextFactory>();

        // Agent execution context factory.
        services.AddScoped<IAgentExecutionContextFactory, AgentExecutionContextFactory>();

        // Error handling.
        services.AddSingleton<IErrorClassifier, DefaultErrorClassifier>();

        // Service validation.
        services.AddSingleton<IServiceProviderValidation, ServiceProviderValidation>();

        // Transport envelope binders for context propagation.
        services.AddSingleton<ITransportEnvelopeBinder, HttpResponseBinder>();
        services.AddSingleton<ITransportEnvelopeBinder, MessagePropertiesBinder>();
        services.AddSingleton<ITransportEnvelopeBinder, JobMetadataBinder>();

        // Default GridContext factory (scoped); downstream middleware will override correlation/causation.
        services.AddScoped<IGridContext>(sp =>
        {
            var nc = sp.GetRequiredService<INodeContext>();
            var factory = sp.GetRequiredService<IGridContextFactory>();
            return factory.CreateRoot(
                nodeId: nc.NodeId,
                studioId: nc.StudioId,
                environment: nc.Environment);
        });

        // Lifecycle coordination (startup/shutdown hooks, health/readiness aggregation).
        services.AddSingleton<NodeLifecycleManager>(sp =>
        {
            var nodeContext = sp.GetRequiredService<INodeContext>();
            var healthContributors = sp.GetServices<IHealthContributor>();
            var readinessContributors = sp.GetServices<IReadinessContributor>();
            var logger = sp.GetRequiredService<ILogger<NodeLifecycleManager>>();
            return new NodeLifecycleManager(nodeContext, healthContributors, readinessContributors, logger);
        });
        services.AddHostedService<NodeLifecycleHost>();

        // Telemetry primitives (OpenTelemetry ActivitySource for distributed tracing).
        services.AddSingleton(GridActivitySource.Instance);

        return new HoneyDrunkBuilder(services);
    }

    private sealed class HoneyDrunkBuilder(IServiceCollection services) : IHoneyDrunkBuilder
    {
        public IServiceCollection Services { get; } = services;
    }

    /// <summary>
    /// Minimal <see cref="INodeDescriptor"/> implementation for bootstrap; extended later by capability registration.
    /// </summary>
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Constructed via DI factory above.")]
    private sealed class NodeDescriptor(string nodeId, string version, string name, string description, string sector, string studioId, string environment, IReadOnlyDictionary<string, string> tags) : INodeDescriptor
    {
        public string NodeId { get; } = nodeId;

        public string Version { get; } = version;

        public string Name { get; } = name;

        public string Description { get; } = description;

        public string? Sector { get; } = sector;

        public string? Cluster => null; // Future: cluster assignment

        public IReadOnlyList<INodeCapability> Capabilities { get; } = [];

        public IReadOnlyList<string> Dependencies { get; } = [];

        public IReadOnlyList<string> Slots { get; } = [];

        public IReadOnlyDictionary<string, string> Tags { get; } = new Dictionary<string, string>(tags);

        public INodeManifest? Manifest => null; // Future: manifest binding

        public string StudioId { get; } = studioId;

        public string Environment { get; } = environment;

        public bool HasCapability(string capabilityName) => false; // No capabilities yet.
    }
}
