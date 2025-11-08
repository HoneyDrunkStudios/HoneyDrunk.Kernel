// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Diagnostics;
using HoneyDrunk.Kernel.Abstractions.Ids;
using HoneyDrunk.Kernel.Abstractions.Time;
using HoneyDrunk.Kernel.Context;
using HoneyDrunk.Kernel.Diagnostics;
using HoneyDrunk.Kernel.Ids;
using HoneyDrunk.Kernel.Time;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Kernel.DI;

/// <summary>
/// Extension methods for registering Kernel services.
/// </summary>
public static class KernelServiceCollectionExtensions
{
    /// <summary>
    /// Registers default Kernel services (clock, ID generator, context, metrics).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKernelDefaults(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IIdGenerator, UlidGenerator>();
        services.AddSingleton<IMetricsCollector, NoOpMetricsCollector>();
        services.AddScoped<IKernelContext>(provider =>
        {
            var idGenerator = provider.GetRequiredService<IIdGenerator>();
            return new KernelContext(
                correlationId: idGenerator.NewString(),
                causationId: null,
                baggage: new Dictionary<string, string>(),
                cancellation: default);
        });

        return services;
    }
}
