using HoneyDrunk.Kernel.Abstractions.Configuration;
using HoneyDrunk.Kernel.Abstractions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Kernel.Configuration;

/// <summary>
/// Extensions for binding HoneyDrunk Grid configuration roots.
/// </summary>
public static class HoneyDrunkConfigurationExtensions
{
    /// <summary>
    /// Binds <see cref="HoneyDrunkGridOptions"/> from configuration (default section 'HoneyDrunk:Grid') and registers it.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration root.</param>
    /// <param name="sectionName">The configuration section name (default: 'HoneyDrunk:Grid').</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHoneyDrunkGridConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "HoneyDrunk:Grid")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(sectionName);
        var options = new HoneyDrunkGridOptions();
        section.Bind(options);
        options.Validate();

        services.AddSingleton(options);
        services.AddSingleton<IOptions<HoneyDrunkGridOptions>>(_ => new OptionsWrapper<HoneyDrunkGridOptions>(options));
        return services;
    }

    /// <summary>
    /// Fluent builder variant for binding <see cref="HoneyDrunkGridOptions"/>.
    /// </summary>
    /// <param name="builder">The HoneyDrunk builder.</param>
    /// <param name="configuration">The configuration root.</param>
    /// <param name="sectionName">The configuration section name (default: 'HoneyDrunk:Grid').</param>
    /// <returns>The builder for chaining.</returns>
    public static IHoneyDrunkBuilder AddGridConfiguration(
        this IHoneyDrunkBuilder builder,
        IConfiguration configuration,
        string sectionName = "HoneyDrunk:Grid")
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddHoneyDrunkGridConfiguration(configuration, sectionName);
        return builder;
    }
}
