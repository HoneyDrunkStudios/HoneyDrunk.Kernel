using AwesomeAssertions;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Hosting;
using HoneyDrunk.Kernel.DependencyInjection;
using HoneyDrunk.Kernel.Lifecycle;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Kernel.Tests.DependencyInjection;

public class HoneyDrunkCoreExtensionsTests
{
    [Fact]
    public void AddHoneyDrunkCoreNode_NullServices_ThrowsArgumentNullException()
    {
        var descriptor = new StubDescriptor();
        IServiceCollection? services = null;

        var act = () => services!.AddHoneyDrunkCoreNode(descriptor);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddHoneyDrunkCoreNode_NullDescriptor_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        var act = () => services.AddHoneyDrunkCoreNode(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddHoneyDrunkCoreNode_EmptyNodeId_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var descriptor = new StubDescriptor { NodeId = string.Empty };

        var act = () => services.AddHoneyDrunkCoreNode(descriptor);

        act.Should().Throw<InvalidOperationException>().WithMessage("*NodeId*");
    }

    [Fact]
    public void AddHoneyDrunkCoreNode_EmptyVersion_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var descriptor = new StubDescriptor { Version = string.Empty };

        var act = () => services.AddHoneyDrunkCoreNode(descriptor);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Version*");
    }

    [Fact]
    public void AddHoneyDrunkCoreNode_ValidDescriptor_RegistersNodeDescriptor()
    {
        var services = new ServiceCollection();
        var descriptor = new StubDescriptor();

        services.AddHoneyDrunkCoreNode(descriptor);
        using var provider = services.BuildServiceProvider();

        provider.GetService<INodeDescriptor>().Should().BeSameAs(descriptor);
    }

    [Fact]
    public void AddHoneyDrunkCoreNode_ValidDescriptor_RegistersNodeContext()
    {
        var services = new ServiceCollection();
        var descriptor = new StubDescriptor();

        services.AddHoneyDrunkCoreNode(descriptor);
        using var provider = services.BuildServiceProvider();

        var ctx = provider.GetService<INodeContext>();
        ctx.Should().NotBeNull();
        ctx!.NodeId.Should().Be(descriptor.NodeId);
        ctx.Version.Should().Be(descriptor.Version);
    }

    [Fact]
    public void AddHoneyDrunkCoreNode_ValidDescriptor_RegistersGridContextAccessorAndLifecycleManager()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddLogging();
        var descriptor = new StubDescriptor();

        services.AddHoneyDrunkCoreNode(descriptor);
        using var provider = services.BuildServiceProvider();

        provider.GetService<IGridContextAccessor>().Should().NotBeNull();
        provider.GetService<NodeLifecycleManager>().Should().NotBeNull();
    }

    [Fact]
    public void ValidateHoneyDrunkServices_NullServiceProvider_ThrowsArgumentNullException()
    {
        IServiceProvider? services = null;

        var act = () => services!.ValidateHoneyDrunkServices();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ValidateHoneyDrunkServices_NoValidator_NoOps()
    {
        // No IServiceProviderValidation registered → method should return without throwing.
        var services = new ServiceCollection();
        using var provider = services.BuildServiceProvider();

        // If this throws, xUnit fails the test — no assertion library wrapper needed.
        provider.ValidateHoneyDrunkServices();
    }

    private sealed class StubDescriptor : INodeDescriptor
    {
        public string NodeId { get; set; } = "test-node";

        public string Version { get; set; } = "1.0.0";

        public string Name => "Test Node";

        public string Description => "Stub descriptor for tests";

        public string? Sector => "core";

        public string? Cluster => null;

        public IReadOnlyList<INodeCapability> Capabilities { get; } = [];

        public IReadOnlyList<string> Dependencies { get; } = [];

        public IReadOnlyList<string> Slots { get; } = [];

        public IReadOnlyDictionary<string, string> Tags { get; } = new Dictionary<string, string>();

        public INodeManifest? Manifest => null;

        public string StudioId { get; } = "test-studio";

        public string Environment { get; } = "test";

        public bool HasCapability(string name) => false;
    }
}
