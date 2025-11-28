using HoneyDrunk.Kernel.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Kernel.Hosting;

/// <summary>
/// Extension methods for service provider validation.
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Validates that all required HoneyDrunk services are properly registered.
    /// </summary>
    /// <param name="serviceProvider">The service provider to validate.</param>
    /// <exception cref="InvalidOperationException">Thrown when required services are missing.</exception>
    /// <remarks>
    /// This method should be called during application startup after all services are registered
    /// but before the application begins processing requests. It validates that core Kernel services
    /// (NodeContext, GridContextAccessor, OperationContext, etc.) are properly configured.
    /// </remarks>
    public static void ValidateHoneyDrunkServices(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var validation = serviceProvider.GetService<IServiceProviderValidation>();

        // Register default validation if not present
        validation ??= new ServiceProviderValidation();

        validation.Validate(serviceProvider);
    }
}
