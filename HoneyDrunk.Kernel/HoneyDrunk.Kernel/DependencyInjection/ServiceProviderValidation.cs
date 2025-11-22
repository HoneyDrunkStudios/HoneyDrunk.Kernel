using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Hosting;
using Microsoft.Extensions.DependencyInjection;

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
        // Validate required services
        EnsureServiceRegistered<INodeContext>(services, "INodeContext");
        EnsureServiceRegistered<IGridContextAccessor>(services, "IGridContextAccessor");

        // Validate optional but recommended services
        WarnIfServiceMissing<IStudioConfiguration>(services, "IStudioConfiguration");
    }

    private static void EnsureServiceRegistered<T>(IServiceProvider services, string serviceName)
    {
        _ = services.GetService<T>() ?? throw new InvalidOperationException(
                $"Required service {serviceName} is not registered. " +
                $"Call AddHoneyDrunkCoreNode() to register core services.");
    }

    private static void WarnIfServiceMissing<T>(IServiceProvider services, string serviceName)
    {
        var service = services.GetService<T>();
        if (service == null)
        {
            // In a real implementation, this would use ILogger
            // For now, we just skip the warning in the validation phase
            Console.WriteLine($"Warning: Recommended service {serviceName} is not registered.");
        }
    }
}
