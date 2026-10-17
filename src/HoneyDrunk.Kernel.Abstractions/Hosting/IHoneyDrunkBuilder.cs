using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Kernel.Abstractions.Hosting;

/// <summary>
/// Fluent builder returned by HoneyDrunk bootstrap to allow progressive registration (telemetry, lifecycle, validation).
/// </summary>
public interface IHoneyDrunkBuilder
{
    /// <summary>
    /// Gets the underlying service collection for advanced customization.
    /// </summary>
    IServiceCollection Services { get; }
}
