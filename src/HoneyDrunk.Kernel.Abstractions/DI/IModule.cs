using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Kernel.Abstractions.DI;

/// <summary>
/// Represents a module that can register services with the DI container.
/// </summary>
public interface IModule
{
    /// <summary>
    /// Registers services with the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    void RegisterServices(IServiceCollection services, IConfiguration configuration);
}
