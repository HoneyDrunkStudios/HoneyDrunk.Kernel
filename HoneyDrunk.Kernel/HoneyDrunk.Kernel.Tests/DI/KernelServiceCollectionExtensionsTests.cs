using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Diagnostics;
using HoneyDrunk.Kernel.Abstractions.Ids;
using HoneyDrunk.Kernel.Abstractions.Time;
using HoneyDrunk.Kernel.DI;
using HoneyDrunk.Kernel.Diagnostics;
using HoneyDrunk.Kernel.Ids;
using HoneyDrunk.Kernel.Time;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Kernel.Tests.DI;

/// <summary>
/// Tests for <see cref="KernelServiceCollectionExtensions"/> registration.
/// </summary>
public class KernelServiceCollectionExtensionsTests
{
    /// <summary>
    /// Validates default service registrations.
    /// </summary>
    [Fact]
    public void AddKernelDefaults_RegistersExpectedServices()
    {
        var services = new ServiceCollection();

        services.AddKernelDefaults();
        using var provider = services.BuildServiceProvider();

        var clock = provider.GetRequiredService<IClock>();
        var idGen = provider.GetRequiredService<IIdGenerator>();
        var metrics = provider.GetRequiredService<IMetricsCollector>();

        clock.Should().BeOfType<SystemClock>();
        idGen.Should().BeOfType<UlidGenerator>();
        metrics.Should().BeOfType<NoOpMetricsCollector>();
    }

    /// <summary>
    /// Validates lifetimes: singletons and scoped context.
    /// </summary>
    [Fact]
    public void AddKernelDefaults_RegistersSingletons_AndScopedContext()
    {
        var services = new ServiceCollection();
        services.AddKernelDefaults();

        using var provider = services.BuildServiceProvider();

        var clock1 = provider.GetRequiredService<IClock>();
        var clock2 = provider.GetRequiredService<IClock>();
        clock1.Should().BeSameAs(clock2);

        var gen1 = provider.GetRequiredService<IIdGenerator>();
        var gen2 = provider.GetRequiredService<IIdGenerator>();
        gen1.Should().BeSameAs(gen2);

        var metrics1 = provider.GetRequiredService<IMetricsCollector>();
        var metrics2 = provider.GetRequiredService<IMetricsCollector>();
        metrics1.Should().BeSameAs(metrics2);

        using var scopeA = provider.CreateScope();
        using var scopeB = provider.CreateScope();

        var ctxA1 = scopeA.ServiceProvider.GetRequiredService<IKernelContext>();
        var ctxA2 = scopeA.ServiceProvider.GetRequiredService<IKernelContext>();
        var ctxB1 = scopeB.ServiceProvider.GetRequiredService<IKernelContext>();

        ctxA1.Should().BeSameAs(ctxA2);
        ctxA1.Should().NotBeSameAs(ctxB1);
    }

    /// <summary>
    /// Validates that correlation id is produced via id generator.
    /// </summary>
    [Fact]
    public void AddKernelDefaults_ContextCorrelation_ComesFromIdGenerator()
    {
        var services = new ServiceCollection();
        services.AddKernelDefaults();

        var expected = "test-correlation";
        services.AddSingleton<IIdGenerator>(new FixedIdGenerator(expected));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var ctx = scope.ServiceProvider.GetRequiredService<IKernelContext>();
        ctx.CorrelationId.Should().Be(expected);
        ctx.CausationId.Should().BeNull();
        ctx.Baggage.Should().NotBeNull();
    }

    private sealed class FixedIdGenerator(string value) : IIdGenerator
    {
        public string NewString() => value;

        public Guid NewGuid()
        {
            return Guid.Parse("11111111-1111-1111-1111-111111111111");
        }
    }
}
