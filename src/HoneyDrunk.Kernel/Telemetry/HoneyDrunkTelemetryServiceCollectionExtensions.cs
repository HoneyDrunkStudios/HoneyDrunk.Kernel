using HoneyDrunk.Kernel.Abstractions.Hosting;
using HoneyDrunk.Kernel.Abstractions.Telemetry;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Kernel.Telemetry;

/// <summary>
/// Service collection extensions for registering HoneyDrunk telemetry primitives.
/// </summary>
public static class HoneyDrunkTelemetryServiceCollectionExtensions
{
    /// <summary>
    /// Registers the shared ActivitySource and telemetry activity factory.
    /// </summary>
    /// <param name="services">DI service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHoneyDrunkTelemetry(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton(HoneyDrunkTelemetry.ActivitySource);
        services.AddScoped<ITelemetryActivityFactory, TelemetryActivityFactory>();
        return services;
    }

    /// <summary>
    /// Registers telemetry and allows caller customization (e.g., enrichers).
    /// </summary>
    /// <param name="services">DI service collection.</param>
    /// <param name="configure">Optional customization action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHoneyDrunkTelemetry(this IServiceCollection services, Action<IServiceCollection>? configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddHoneyDrunkTelemetry();
        configure?.Invoke(services);
        return services;
    }

    /// <summary>
    /// Fluent helper for <see cref="IHoneyDrunkBuilder"/> to add telemetry primitives.
    /// </summary>
    /// <param name="builder">HoneyDrunk builder.</param>
    /// <returns>Original builder for chaining.</returns>
    public static IHoneyDrunkBuilder AddTelemetry(this IHoneyDrunkBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddHoneyDrunkTelemetry();
        return builder;
    }
}
