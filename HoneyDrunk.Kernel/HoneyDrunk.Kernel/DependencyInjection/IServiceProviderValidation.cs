namespace HoneyDrunk.Kernel.DependencyInjection;

/// <summary>
/// Internal interface for service provider validation.
/// </summary>
internal interface IServiceProviderValidation
{
    /// <summary>
    /// Validates that required services are registered.
    /// </summary>
    /// <param name="services">The service provider to validate.</param>
    void Validate(IServiceProvider services);
}
