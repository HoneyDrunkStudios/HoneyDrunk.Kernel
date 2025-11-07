using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HoneyDrunk.Kernel.Abstractions.DI;

/// <summary>
/// Defines a modular registration contract for dependency injection.
/// </summary>
public interface IModule
{
    /// <summary>
    /// Registers services into the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to register services into.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="environment">Host environment information.</param>
    void Register(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment);
}
